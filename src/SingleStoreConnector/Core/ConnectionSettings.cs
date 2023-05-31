using System.Net.Security;
using System.Security.Authentication;
using SingleStoreConnector.Logging;
using SingleStoreConnector.Utilities;

namespace SingleStoreConnector.Core;

internal sealed class ConnectionSettings
{
	public ConnectionSettings(SingleStoreConnectionStringBuilder csb)
	{
		ConnectionStringBuilder = csb;
		ConnectionString = csb.ConnectionString;

		if (csb.ConnectionProtocol == SingleStoreConnectionProtocol.UnixSocket || (!Utility.IsWindows() && (csb.Server.StartsWith("/", StringComparison.Ordinal) || csb.Server.StartsWith("./", StringComparison.Ordinal))))
		{
			if (csb.LoadBalance != SingleStoreLoadBalance.RoundRobin)
				throw new NotSupportedException("LoadBalance not supported when ConnectionProtocol=UnixSocket");
			if (!File.Exists(csb.Server))
				throw new SingleStoreException("Cannot find Unix Socket at " + csb.Server);
			ConnectionProtocol = SingleStoreConnectionProtocol.UnixSocket;
			UnixSocket = Path.GetFullPath(csb.Server);
			PipeName = "";
		}
		else if (csb.ConnectionProtocol == SingleStoreConnectionProtocol.NamedPipe)
		{
			if (csb.LoadBalance != SingleStoreLoadBalance.RoundRobin)
				throw new NotSupportedException("LoadBalance not supported when ConnectionProtocol=NamedPipe");
			ConnectionProtocol = SingleStoreConnectionProtocol.NamedPipe;
			HostNames = (csb.Server == "." || string.Equals(csb.Server, "localhost", StringComparison.OrdinalIgnoreCase)) ? s_localhostPipeServer : new[] { csb.Server };
			PipeName = csb.PipeName;
		}
		else if (csb.ConnectionProtocol == SingleStoreConnectionProtocol.SharedMemory)
		{
			throw new NotSupportedException("Shared Memory connections are not supported");
		}
		else
		{
			ConnectionProtocol = SingleStoreConnectionProtocol.Sockets;
			HostNames = csb.Server.Split(',');
			LoadBalance = csb.LoadBalance;
			Port = (int) csb.Port;
			PipeName = "";
		}

		UserID = csb.UserID;
		Password = csb.Password;
		Database = csb.Database;

		// SSL/TLS Options
		SslMode = csb.SslMode;
		CertificateFile = csb.CertificateFile;
		CertificatePassword = csb.CertificatePassword;
		SslCertificateFile = csb.SslCert;
		SslKeyFile = csb.SslKey;
		CACertificateFile = csb.SslCa;
		CertificateStoreLocation = csb.CertificateStoreLocation;
		CertificateThumbprint = csb.CertificateThumbprint;

		if (csb.TlsVersion.Length == 0)
		{
			TlsVersions = Utility.GetDefaultSslProtocols();
		}
		else
		{
			TlsVersions = default;
			for (var i = 6; i < csb.TlsVersion.Length; i += 9)
			{
				char minorVersion = csb.TlsVersion[i];
				if (minorVersion == '0')
					TlsVersions |= SslProtocols.Tls;
				else if (minorVersion == '1')
					TlsVersions |= SslProtocols.Tls11;
				else if (minorVersion == '2')
					TlsVersions |= SslProtocols.Tls12;
#if NETCOREAPP3_0_OR_GREATER
				else if (minorVersion == '3')
					TlsVersions |= SslProtocols.Tls13;
#endif
				else
					throw new InvalidOperationException($"Unexpected character '{minorVersion}' for TLS minor version.");
			}
			if (TlsVersions == default)
				throw new NotSupportedException("All specified TLS versions are incompatible with this platform.");
		}

		if (csb.TlsCipherSuites.Length != 0)
		{
#if NETCOREAPP3_0_OR_GREATER
			var tlsCipherSuites = new List<TlsCipherSuite>();
			foreach (var token in csb.TlsCipherSuites.Split(','))
			{
				var suiteName = token.Trim();
				if (Enum.TryParse<TlsCipherSuite>(suiteName, ignoreCase: true, out var cipherSuite))
					tlsCipherSuites.Add(cipherSuite);
				else if (int.TryParse(suiteName, out var value) && Enum.IsDefined(typeof(TlsCipherSuite), value))
					tlsCipherSuites.Add((TlsCipherSuite) value);
				else if (Enum.TryParse("TLS_" + suiteName, ignoreCase: true, out cipherSuite))
					tlsCipherSuites.Add(cipherSuite);
				else
					throw new NotSupportedException($"Unknown value '{suiteName}' for TlsCipherSuites.");
			}
			TlsCipherSuites = tlsCipherSuites;
#else
			throw new PlatformNotSupportedException("The TlsCipherSuites connection string option is only supported on .NET Core 3.1 (or later) on Linux.");
#endif
		}

		// Connection Pooling Options
		Pooling = csb.Pooling;
		ConnectionLifeTime = Math.Min(csb.ConnectionLifeTime, uint.MaxValue / 1000) * 1000;
		ConnectionReset = csb.ConnectionReset;
		ConnectionIdleTimeout = (int) csb.ConnectionIdleTimeout;
#pragma warning disable 618
		if (!csb.DeferConnectionReset)
			Log.Warn("DeferConnectionReset=false is not supported; using regular connection reset behavior.");
#pragma warning restore 618
		if (csb.MinimumPoolSize > csb.MaximumPoolSize)
			throw new SingleStoreException("MaximumPoolSize must be greater than or equal to MinimumPoolSize");
		MinimumPoolSize = ToSigned(csb.MinimumPoolSize);
		MaximumPoolSize = ToSigned(csb.MaximumPoolSize);
		DnsCheckInterval = ToSigned(csb.DnsCheckInterval);

		// Other Options
		AllowLoadLocalInfile = csb.AllowLoadLocalInfile;
		AllowPublicKeyRetrieval = csb.AllowPublicKeyRetrieval;
		AllowUserVariables = csb.AllowUserVariables;
		AllowZeroDateTime = csb.AllowZeroDateTime;
		ApplicationName = csb.ApplicationName;
		AutoEnlist = csb.AutoEnlist;
		CancellationTimeout = csb.CancellationTimeout;
		ConnAttrsExtra = csb.ConnectionAttributes;
		ConnectionTimeout = ToSigned(csb.ConnectionTimeout);
		ConvertZeroDateTime = csb.ConvertZeroDateTime;
		DateTimeKind = (DateTimeKind) csb.DateTimeKind;
		DefaultCommandTimeout = ToSigned(csb.DefaultCommandTimeout);
		ForceSynchronous = csb.ForceSynchronous;
		IgnoreCommandTransaction = csb.IgnoreCommandTransaction;
		IgnorePrepare = csb.IgnorePrepare;
		InteractiveSession = csb.InteractiveSession;
		TreatChar48AsGeographyPoint = csb.TreatChar48AsGeographyPoint;
		GuidFormat = GetEffectiveGuidFormat(csb.GuidFormat, csb.OldGuids);
		Keepalive = csb.Keepalive;
		NoBackslashEscapes = csb.NoBackslashEscapes;
		PersistSecurityInfo = csb.PersistSecurityInfo;
		Pipelining = csb.ContainsKey("Pipelining") ? csb.Pipelining : default(bool?);
		ServerRedirectionMode = csb.ServerRedirectionMode;
		ServerRsaPublicKeyFile = csb.ServerRsaPublicKeyFile;
		ServerSPN = csb.ServerSPN;
		TreatTinyAsBoolean = csb.TreatTinyAsBoolean;
		UseAffectedRows = csb.UseAffectedRows;
		UseCompression = csb.UseCompression;
		UseXaTransactions = false;

		static int ToSigned(uint value) => value >= int.MaxValue ? int.MaxValue : (int) value;
	}

	public ConnectionSettings CloneWith(string host, int port, string userId) => new ConnectionSettings(this, host, port, userId);

	private static SingleStoreGuidFormat GetEffectiveGuidFormat(SingleStoreGuidFormat guidFormat, bool oldGuids)
	{
		switch (guidFormat)
		{
		case SingleStoreGuidFormat.Default:
			return oldGuids ? SingleStoreGuidFormat.LittleEndianBinary16 : SingleStoreGuidFormat.Char36;
		case SingleStoreGuidFormat.None:
		case SingleStoreGuidFormat.Char36:
		case SingleStoreGuidFormat.Char32:
		case SingleStoreGuidFormat.Binary16:
		case SingleStoreGuidFormat.TimeSwapBinary16:
		case SingleStoreGuidFormat.LittleEndianBinary16:
			if (oldGuids)
				throw new SingleStoreException("OldGuids cannot be used with GuidFormat");
			return guidFormat;
		default:
			throw new SingleStoreException("Unknown GuidFormat");
		}
	}

	/// <summary>
	/// The <see cref="SingleStoreConnectionStringBuilder" /> that was used to create this <see cref="ConnectionSettings" />.!--
	/// This object must not be mutated.
	/// </summary>
	public SingleStoreConnectionStringBuilder ConnectionStringBuilder { get; }

	// Base Options
	public string ConnectionString { get; }
	public SingleStoreConnectionProtocol ConnectionProtocol { get; }
	public IReadOnlyList<string>? HostNames { get; }
	public SingleStoreLoadBalance LoadBalance { get; }
	public int Port { get; }
	public string PipeName { get; }
	public string? UnixSocket { get; }
	public string UserID { get; }
	public string Password { get; }
	public string Database { get; }

	// SSL/TLS Options
	public SingleStoreSslMode SslMode { get; }
	public string CertificateFile { get; }
	public string CertificatePassword { get; }
	public string CACertificateFile { get; }
	public string SslCertificateFile { get; }
	public string SslKeyFile { get; }
	public SingleStoreCertificateStoreLocation CertificateStoreLocation { get; }
	public string CertificateThumbprint { get; }
	public SslProtocols TlsVersions { get; }
#if NETCOREAPP3_0_OR_GREATER
	public IReadOnlyList<TlsCipherSuite>? TlsCipherSuites { get; }
#endif

	// Connection Pooling Options
	public bool Pooling { get; }
	public uint ConnectionLifeTime { get; }
	public bool ConnectionReset { get; }
	public int ConnectionIdleTimeout { get; }
	public int MinimumPoolSize { get; }
	public int MaximumPoolSize { get; }
	public int DnsCheckInterval { get; }

	// Other Options
	public bool AllowLoadLocalInfile { get; }
	public bool AllowPublicKeyRetrieval { get; }
	public bool AllowUserVariables { get; }
	public bool AllowZeroDateTime { get; }
	public string ApplicationName { get; }
	public bool AutoEnlist { get; }
	public int CancellationTimeout { get; }
	public int ConnectionTimeout { get; }
	public bool ConvertZeroDateTime { get; }
	public DateTimeKind DateTimeKind { get; }
	public int DefaultCommandTimeout { get; }
	public bool ForceSynchronous { get; }
	public bool TreatChar48AsGeographyPoint { get; }
	public SingleStoreGuidFormat GuidFormat { get; }
	public bool IgnoreCommandTransaction { get; }
	public bool IgnorePrepare { get; }
	public bool InteractiveSession { get; }
	public uint Keepalive { get; }
	public bool NoBackslashEscapes { get; }
	public bool PersistSecurityInfo { get; }
	public bool? Pipelining { get; }
	public SingleStoreServerRedirectionMode ServerRedirectionMode { get; }
	public string ServerRsaPublicKeyFile { get; }
	public string ServerSPN { get; }
	public bool TreatTinyAsBoolean { get; }
	public bool UseAffectedRows { get; }
	public bool UseCompression { get; }
	public bool UseXaTransactions { get; }

	public string ConnAttrsExtra { get; set; }
	public byte[]? ConnectionAttributes { get; set; }

	// Helper Functions
	private int? m_connectionTimeoutMilliseconds;
	public int ConnectionTimeoutMilliseconds
	{
		get
		{
			if (!m_connectionTimeoutMilliseconds.HasValue)
			{
				try
				{
					checked
					{
						m_connectionTimeoutMilliseconds = ConnectionTimeout * 1000;
					}
				}
				catch (OverflowException)
				{
					m_connectionTimeoutMilliseconds = int.MaxValue;
				}
			}
			return m_connectionTimeoutMilliseconds.Value;
		}
	}

	private ConnectionSettings(ConnectionSettings other, string host, int port, string userId)
	{
		ConnectionStringBuilder = other.ConnectionStringBuilder;
		ConnectionString = other.ConnectionString;

		ConnectionProtocol = SingleStoreConnectionProtocol.Sockets;
		HostNames = new[] { host };
		LoadBalance = other.LoadBalance;
		Port = port;
		PipeName = other.PipeName;

		UserID = userId;
		Password = other.Password;
		Database = other.Database;

		SslMode = other.SslMode;
		CertificateFile = other.CertificateFile;
		CertificatePassword = other.CertificatePassword;
		SslCertificateFile = other.SslCertificateFile;
		SslKeyFile = other.SslKeyFile;
		CACertificateFile = other.CACertificateFile;
		CertificateStoreLocation = other.CertificateStoreLocation;
		CertificateThumbprint = other.CertificateThumbprint;

		Pooling = other.Pooling;
		ConnectionLifeTime = other.ConnectionLifeTime;
		ConnectionReset = other.ConnectionReset;
		ConnectionIdleTimeout = other.ConnectionIdleTimeout;
		MinimumPoolSize = other.MinimumPoolSize;
		MaximumPoolSize = other.MaximumPoolSize;
		DnsCheckInterval = other.DnsCheckInterval;

		AllowLoadLocalInfile = other.AllowLoadLocalInfile;
		AllowPublicKeyRetrieval = other.AllowPublicKeyRetrieval;
		AllowUserVariables = other.AllowUserVariables;
		AllowZeroDateTime = other.AllowZeroDateTime;
		ApplicationName = other.ApplicationName;
		AutoEnlist = other.AutoEnlist;
		ConnAttrsExtra = other.ConnAttrsExtra;
		ConnectionTimeout = other.ConnectionTimeout;
		ConvertZeroDateTime = other.ConvertZeroDateTime;
		DateTimeKind = other.DateTimeKind;
		DefaultCommandTimeout = other.DefaultCommandTimeout;
		ForceSynchronous = other.ForceSynchronous;
		IgnoreCommandTransaction = other.IgnoreCommandTransaction;
		IgnorePrepare = other.IgnorePrepare;
		InteractiveSession = other.InteractiveSession;
		TreatChar48AsGeographyPoint = other.TreatChar48AsGeographyPoint;
		GuidFormat = other.GuidFormat;
		Keepalive = other.Keepalive;
		NoBackslashEscapes = other.NoBackslashEscapes;
		PersistSecurityInfo = other.PersistSecurityInfo;
		Pipelining = other.Pipelining;
		ServerRedirectionMode = other.ServerRedirectionMode;
		ServerRsaPublicKeyFile = other.ServerRsaPublicKeyFile;
		ServerSPN = other.ServerSPN;
		TreatTinyAsBoolean = other.TreatTinyAsBoolean;
		UseAffectedRows = other.UseAffectedRows;
		UseCompression = other.UseCompression;
		UseXaTransactions = other.UseXaTransactions;
	}

	private static readonly ISingleStoreConnectorLogger Log = SingleStoreConnectorLogManager.CreateLogger(nameof(ConnectionSettings));
	private static readonly string[] s_localhostPipeServer = { "." };
}
