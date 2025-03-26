using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace SideBySide;

public class SslTests : IClassFixture<DatabaseFixture>
{
	public SslTests(DatabaseFixture database)
	{
		m_database = database;
	}

#if !BASELINE
	[SkippableFact(ConfigSettings.RequiresSsl)]
	public async Task ConnectSslPreferred()
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		csb.SslMode = SingleStoreSslMode.Preferred;
		csb.CertificateFile = null;
		csb.CertificatePassword = null;
		using var connection = new SingleStoreConnection(csb.ConnectionString);
		using var cmd = connection.CreateCommand();
		await connection.OpenAsync();
		Assert.True(connection.SslIsEncrypted);
		Assert.True(connection.SslIsSigned);
		Assert.True(connection.SslIsAuthenticated);
		Assert.False(connection.SslIsMutuallyAuthenticated);
		cmd.CommandText = "SHOW SESSION STATUS LIKE 'Ssl_version'";
		var sslVersion = (string) await cmd.ExecuteScalarAsync();
		Assert.False(string.IsNullOrWhiteSpace(sslVersion));
	}
#endif

	[SkippableTheory(ConfigSettings.RequiresSsl | ConfigSettings.KnownClientCertificate)]
	[InlineData("ssl-client.pfx", null, null)]
	[InlineData("ssl-client-pw-test.pfx", "test", null)]
#if !BASELINE
	[InlineData("ssl-client.pfx", null, "ssl-ca-cert.pem")]
	[InlineData("ssl-client-pw-test.pfx", "test", "ssl-ca-cert.pem")]
#endif
	public async Task ConnectSslClientCertificate(string certFile, string certFilePassword, string caCertFile)
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		csb.CertificateFile = Path.Combine(AppConfig.CertsPath, certFile);
		csb.CertificatePassword = certFilePassword;
		if (caCertFile is not null)
		{
			csb.SslMode = SingleStoreSslMode.VerifyCA;
			csb.SslCa = Path.Combine(AppConfig.CertsPath, caCertFile);
		}
		await DoTestSsl(csb.ConnectionString);
	}

#if !BASELINE
	[SkippableTheory(ConfigSettings.RequiresSsl | ConfigSettings.KnownClientCertificate)]
	[InlineData("ssl-client.pfx", null)]
	[InlineData("ssl-client-pw-test.pfx", "test")]
	public async Task ConnectSslClientCertificateCallback(string certificateFile, string certificateFilePassword)
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		var certificateFilePath = Path.Combine(AppConfig.CertsPath, certificateFile);

		using var connection = new SingleStoreConnection(csb.ConnectionString);
		connection.ProvideClientCertificatesCallback = x =>
		{
			x.Add(new X509Certificate2(certificateFilePath, certificateFilePassword));
			return default;
		};

		await connection.OpenAsync();
		Assert.True(connection.SslIsEncrypted);
	}
#endif

	[SkippableTheory(ConfigSettings.RequiresSsl | ConfigSettings.KnownClientCertificate)]
	[InlineData("ssl-client-cert.pem", "ssl-client-key.pem", null)]
	[InlineData("ssl-client-cert.pem", "ssl-client-key-null.pem", null)]
#if !BASELINE
	[InlineData("ssl-client-cert.pem", "ssl-client-key.pem", "ssl-ca-cert.pem")] // https://bugs.mysql.com/bug.php?id=95436
	[InlineData("ssl-client-cert.pem", "ssl-client-key-null.pem", "ssl-ca-cert.pem")] // https://bugs.mysql.com/bug.php?id=95436
#endif
	public async Task ConnectSslClientCertificatePem(string certFile, string keyFile, string caCertFile)
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		csb.CertificateFile = null;
		csb.SslCert = Path.Combine(AppConfig.CertsPath, certFile);
		csb.SslKey = Path.Combine(AppConfig.CertsPath, keyFile);
		if (caCertFile is not null)
		{
			csb.SslMode = SingleStoreSslMode.VerifyCA;
			csb.SslCa = Path.Combine(AppConfig.CertsPath, caCertFile);
		}
		await DoTestSsl(csb.ConnectionString);
	}

	private async Task DoTestSsl(string connectionString)
	{
		using var connection = new SingleStoreConnection(connectionString);
		using var cmd = connection.CreateCommand();
		await connection.OpenAsync();
#if !BASELINE
		Assert.True(connection.SslIsEncrypted);
		Assert.True(connection.SslIsSigned);
		Assert.True(connection.SslIsAuthenticated);
		Assert.True(connection.SslIsMutuallyAuthenticated);
#endif
		cmd.CommandText = "SHOW SESSION STATUS LIKE 'Ssl_version'";
		var sslVersion = (string) await cmd.ExecuteScalarAsync();
		Assert.False(string.IsNullOrWhiteSpace(sslVersion));
	}

	[SkippableTheory(ConfigSettings.RequiresSsl | ConfigSettings.KnownClientCertificate)]
	[InlineData("ssl-client.pfx", SingleStoreCertificateStoreLocation.CurrentUser, null)]
	public async Task ConnectSslClientCertificateFromCertificateStore(string certFile, SingleStoreCertificateStoreLocation storeLocation, string thumbprint)
	{
		// Create a mock of certificate store
		var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
		store.Open(OpenFlags.ReadWrite);
		var certificate = new X509Certificate2(Path.Combine(AppConfig.CertsPath, certFile));
		store.Add(certificate);

		var csb = AppConfig.CreateConnectionStringBuilder();

		csb.CertificateStoreLocation = storeLocation;
		csb.CertificateThumbprint = thumbprint;

		using (var connection = new SingleStoreConnection(csb.ConnectionString))
		{
			using var cmd = connection.CreateCommand();
			await connection.OpenAsync();
#if !BASELINE
			Assert.True(connection.SslIsEncrypted);
			Assert.True(connection.SslIsSigned);
			Assert.True(connection.SslIsAuthenticated);
			Assert.True(connection.SslIsMutuallyAuthenticated);
#endif
			cmd.CommandText = "SHOW SESSION STATUS LIKE 'Ssl_version'";
			var sslVersion = (string) await cmd.ExecuteScalarAsync();
			Assert.False(string.IsNullOrWhiteSpace(sslVersion));
		}

		// Remove the certificate from store
		store.Remove(certificate);
	}

	[SkippableFact(ConfigSettings.RequiresSsl, Baseline = "MySql.Data does not check for a private key")]
	public async Task ConnectSslClientCertificateNoPrivateKey()
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		csb.CertificateFile = Path.Combine(AppConfig.CertsPath, "ssl-client-cert.pem");
		csb.SslMode = SingleStoreSslMode.Required;
		using var connection = new SingleStoreConnection(csb.ConnectionString);
		await Assert.ThrowsAsync<SingleStoreException>(async () => await connection.OpenAsync());
	}

	[SkippableFact(ServerFeatures.KnownCertificateAuthority, ConfigSettings.RequiresSsl)]
	public async Task ConnectSslBadClientCertificate()
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		csb.CertificateFile = Path.Combine(AppConfig.CertsPath, "non-ca-client.pfx");
		csb.CertificatePassword = "";
		using var connection = new SingleStoreConnection(csb.ConnectionString);
#if !BASELINE
		await Assert.ThrowsAsync<SingleStoreException>(async () => await connection.OpenAsync());
#else
		await Assert.ThrowsAsync<AuthenticationException>(async () => await connection.OpenAsync());
#endif
	}

	[SkippableFact(ServerFeatures.KnownCertificateAuthority, ConfigSettings.RequiresSsl)]
	public async Task ConnectSslBadCaCertificate()
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
#if !BASELINE
		csb.CertificateFile = Path.Combine(AppConfig.CertsPath, "ssl-client.pfx");
#else
		csb.SslCert = Path.Combine(AppConfig.CertsPath, "ssl-client-cert.pem");
		csb.SslKey = Path.Combine(AppConfig.CertsPath, "ssl-client-key.pem");
#endif
		csb.SslMode = SingleStoreSslMode.VerifyCA;
		csb.SslCa = Path.Combine(AppConfig.CertsPath, "non-ca-client-cert.pem");
		using var connection = new SingleStoreConnection(csb.ConnectionString);
		await Assert.ThrowsAsync<SingleStoreException>(async () => await connection.OpenAsync());
	}

#if !BASELINE
	[SkippableTheory(ServerFeatures.KnownCertificateAuthority, ConfigSettings.RequiresSsl)]
	[InlineData(SingleStoreSslMode.VerifyCA, false, false)]
	[InlineData(SingleStoreSslMode.VerifyCA, true, false)]
	[InlineData(SingleStoreSslMode.Required, true, true)]
	public async Task ConnectSslRemoteCertificateValidationCallback(SingleStoreSslMode sslMode, bool clearCA, bool expectedSuccess)
	{
		var csb = AppConfig.CreateConnectionStringBuilder();
		csb.CertificateFile = Path.Combine(AppConfig.CertsPath, "ssl-client.pfx");
		csb.SslMode = sslMode;
		csb.SslCa = clearCA ? "" : Path.Combine(AppConfig.CertsPath, "non-ca-client-cert.pem");
		using var connection = new SingleStoreConnection(csb.ConnectionString);
		connection.RemoteCertificateValidationCallback = (s, c, h, e) => true;

		if (expectedSuccess)
			await connection.OpenAsync();
		else
			await Assert.ThrowsAsync<SingleStoreException>(async () => await connection.OpenAsync());
	}
#endif

	[SkippableFact(ConfigSettings.RequiresSsl)]
	public async Task ConnectSslTlsVersion()
	{
		using var connection = new SingleStoreConnection(AppConfig.ConnectionString);
		await connection.OpenAsync();
#pragma warning disable SYSLIB0039 // SslProtocols.Tls11 is obsolete
		var expectedProtocol = AppConfig.SupportedFeatures.HasFlag(ServerFeatures.Tls12) ? SslProtocols.Tls12 :
			AppConfig.SupportedFeatures.HasFlag(ServerFeatures.Tls11) ? SslProtocols.Tls11 :
			SslProtocols.Tls;
		var expectedProtocolString = expectedProtocol == SslProtocols.Tls12 ? "TLSv1.2" :
			expectedProtocol == SslProtocols.Tls11 ? "TLSv1.1" : "TLSv1";
#pragma warning restore SYSLIB0039 // SslProtocols.Tls11 is obsolete

#if !NET462 && !NET472
		// https://docs.microsoft.com/en-us/dotnet/core/whats-new/dotnet-core-3-0#tls-13--openssl-111-on-linux
		if (expectedProtocol == SslProtocols.Tls12 && AppConfig.SupportedFeatures.HasFlag(ServerFeatures.Tls13) && RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			expectedProtocol = SslProtocols.Tls13;
			expectedProtocolString = "TLSv1.3";
		}
#endif

#if !BASELINE
		Assert.Equal(expectedProtocol, connection.SslProtocol);
#endif
		using var cmd = new SingleStoreCommand("show status like 'Ssl_version';", connection);
		using var reader = await cmd.ExecuteReaderAsync();
		Assert.True(reader.Read());
		Assert.Equal(expectedProtocolString, reader.GetString(1));
	}

#if !NET5_0_OR_GREATER
	[SkippableFact(ConfigSettings.RequiresSsl)]
	public async Task ForceTls11()
	{
		// require TLS 1.1 and TLS 1.2
		if (!AppConfig.SupportedFeatures.HasFlag(ServerFeatures.Tls11) || !AppConfig.SupportedFeatures.HasFlag(ServerFeatures.Tls12))
			return;

		var csb = AppConfig.CreateConnectionStringBuilder();
		csb.TlsVersion = "TLS 1.1";

		using var connection = new SingleStoreConnection(csb.ConnectionString);
		await connection.OpenAsync();

#if !BASELINE
		Assert.Equal(SslProtocols.Tls11, connection.SslProtocol);
#endif
		using var cmd = new SingleStoreCommand("show status like 'Ssl_version';", connection);
		using var reader = await cmd.ExecuteReaderAsync();
		Assert.True(reader.Read());
		Assert.Equal("TLSv1.1", reader.GetString(1));
	}
#endif

	readonly DatabaseFixture m_database;
}
