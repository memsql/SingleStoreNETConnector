using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using SingleStoreConnector.Utilities;

namespace SingleStoreConnector;

#pragma warning disable CA1010 // Generic interface should also be implemented

/// <summary>
/// <see cref="SingleStoreConnectionStringBuilder"/> allows you to construct a SingleStore connection string by setting properties on the builder then reading the <see cref="DbConnectionStringBuilder.ConnectionString"/> property.
/// </summary>
/// <remarks>See <a href="https://mysqlconnector.net/connection-options/">Connection String Options</a> for more documentation on the options.</remarks>
public sealed class SingleStoreConnectionStringBuilder : DbConnectionStringBuilder
{
	/// <summary>
	/// Initializes a new <see cref="SingleStoreConnectionStringBuilder"/>.
	/// </summary>
	public SingleStoreConnectionStringBuilder()
	{
	}

	/// <summary>
	/// Initializes a new <see cref="SingleStoreConnectionStringBuilder"/> with properties set from the specified connection string.
	/// </summary>
	/// <param name="connectionString">The connection string to use to set property values on this object.</param>
	public SingleStoreConnectionStringBuilder(string connectionString)
	{
		ConnectionString = connectionString;
	}

	// Connection Options

	/// <summary>
	/// <para>The host name or network address of the SingleStore Server to which to connect. Multiple hosts can be specified in a comma-delimited list.</para>
	/// <para>On Unix-like systems, this can be a fully qualified path to a SingleStore socket file, which will cause a Unix socket to be used instead of a TCP/IP socket. Only a single socket name can be specified.</para>
	/// </summary>
	[AllowNull]
	[Category("Connection")]
	[DefaultValue("")]
	[Description("The host name or network address of the SingleStore Server to which to connect.")]
	[DisplayName("Server")]
	public string Server
	{
		get => SingleStoreConnectionStringOption.Server.GetValue(this);
		set => SingleStoreConnectionStringOption.Server.SetValue(this, value);
	}

	/// <summary>
	/// The TCP port on which SingleStore Server is listening for connections.
	/// </summary>
	[Category("Connection")]
	[DefaultValue(3306u)]
	[Description("The TCP port on which SingleStore Server is listening for connections.")]
	[DisplayName("Port")]
	public uint Port
	{
		get => SingleStoreConnectionStringOption.Port.GetValue(this);
		set => SingleStoreConnectionStringOption.Port.SetValue(this, value);
	}

	/// <summary>
	/// The SingleStore user ID.
	/// </summary>
	[AllowNull]
	[Category("Connection")]
	[DefaultValue("")]
	[Description("The SingleStore user ID.")]
	[DisplayName("User ID")]
	public string UserID
	{
		get => SingleStoreConnectionStringOption.UserID.GetValue(this);
		set => SingleStoreConnectionStringOption.UserID.SetValue(this, value);
	}

	/// <summary>
	/// The password for the SingleStore user.
	/// </summary>
	[AllowNull]
	[Category("Connection")]
	[DefaultValue("")]
	[Description("The password for the SingleStore user.")]
	[DisplayName("Password")]
	public string Password
	{
		get => SingleStoreConnectionStringOption.Password.GetValue(this);
		set => SingleStoreConnectionStringOption.Password.SetValue(this, value);
	}

	/// <summary>
	/// (Optional) The case-sensitive name of the initial database to use. This may be required if the SingleStore user account only has access rights to particular databases on the server.
	/// </summary>
	[AllowNull]
	[Category("Connection")]
	[DefaultValue("The case-sensitive name of the initial database to use.")]
	[Description("The case-sensitive name of the initial database to use")]
	[DisplayName("Database")]
	public string Database
	{
		get => SingleStoreConnectionStringOption.Database.GetValue(this);
		set => SingleStoreConnectionStringOption.Database.SetValue(this, value);
	}

	/// <summary>
	/// Specifies how load is distributed across backend servers.
	/// </summary>
	[Category("Connection")]
	[DefaultValue(SingleStoreLoadBalance.RoundRobin)]
	[Description("Specifies how load is distributed across backend servers.")]
	[DisplayName("Load Balance")]
	public SingleStoreLoadBalance LoadBalance
	{
		get => SingleStoreConnectionStringOption.LoadBalance.GetValue(this);
		set => SingleStoreConnectionStringOption.LoadBalance.SetValue(this, value);
	}

	/// <summary>
	/// The protocol to use to connect to the SingleStore Server.
	/// </summary>
	[Category("Connection")]
	[DefaultValue(SingleStoreConnectionProtocol.Socket)]
	[Description("The protocol to use to connect to the SingleStore Server.")]
	[DisplayName("Connection Protocol")]
	public SingleStoreConnectionProtocol ConnectionProtocol
	{
		get => SingleStoreConnectionStringOption.ConnectionProtocol.GetValue(this);
		set => SingleStoreConnectionStringOption.ConnectionProtocol.SetValue(this, value);
	}

	/// <summary>
	/// The name of the Windows named pipe to use to connect to the server. You must also set <see cref="ConnectionProtocol"/> to <see cref="SingleStoreConnectionProtocol.NamedPipe"/> to used named pipes.
	/// </summary>
	[AllowNull]
	[Category("Connection")]
	[DefaultValue("MYSQL")]
	[Description("The name of the Windows named pipe to use to connect to the server.")]
	[DisplayName("Pipe Name")]
	public string PipeName
	{
		get => SingleStoreConnectionStringOption.PipeName.GetValue(this);
		set => SingleStoreConnectionStringOption.PipeName.SetValue(this, value);
	}

	// SSL/TLS Options

	/// <summary>
	/// Whether to use SSL/TLS when connecting to the SingleStore server.
	/// </summary>
	[Category("TLS")]
	[DefaultValue(SingleStoreSslMode.Preferred)]
	[Description("Whether to use SSL/TLS when connecting to the SingleStore server.")]
	[DisplayName("SSL Mode")]
	public SingleStoreSslMode SslMode
	{
		get => SingleStoreConnectionStringOption.SslMode.GetValue(this);
		set => SingleStoreConnectionStringOption.SslMode.SetValue(this, value);
	}

	/// <summary>
	/// The path to a certificate file in PKCS #12 (.pfx) format containing a bundled Certificate and Private Key used for mutual authentication.
	/// </summary>
	[AllowNull]
	[Category("TLS")]
	[DefaultValue("")]
	[Description("The path to a certificate file in PKCS #12 (.pfx) format containing a bundled Certificate and Private Key used for mutual authentication.")]
	[DisplayName("Certificate File")]
	public string CertificateFile
	{
		get => SingleStoreConnectionStringOption.CertificateFile.GetValue(this);
		set => SingleStoreConnectionStringOption.CertificateFile.SetValue(this, value);
	}

	/// <summary>
	/// The password for the certificate specified using the <see cref="CertificateFile"/> option. Not required if the certificate file is not password protected.
	/// </summary>
	[AllowNull]
	[Category("TLS")]
	[DefaultValue("")]
	[Description("The password for the certificate specified using the Certificate File option.")]
	[DisplayName("Certificate Password")]
	public string CertificatePassword
	{
		get => SingleStoreConnectionStringOption.CertificatePassword.GetValue(this);
		set => SingleStoreConnectionStringOption.CertificatePassword.SetValue(this, value);
	}

	/// <summary>
	/// Uses a certificate from the specified Certificate Store on the machine. The default value of <see cref="SingleStoreCertificateStoreLocation.None"/> means the certificate store is not used; a value of <see cref="SingleStoreCertificateStoreLocation.CurrentUser"/> or <see cref="SingleStoreCertificateStoreLocation.LocalMachine"/> uses the specified store.
	/// </summary>
	[Category("TLS")]
	[DefaultValue(SingleStoreCertificateStoreLocation.None)]
	[Description("Uses a certificate from the specified Certificate Store on the machine.")]
	[DisplayName("Certificate Store Location")]
	public SingleStoreCertificateStoreLocation CertificateStoreLocation
	{
		get => SingleStoreConnectionStringOption.CertificateStoreLocation.GetValue(this);
		set => SingleStoreConnectionStringOption.CertificateStoreLocation.SetValue(this, value);
	}

	/// <summary>
	/// Specifies which certificate should be used from the Certificate Store specified in <see cref="CertificateStoreLocation"/>. This option must be used to indicate which certificate in the store should be used for authentication.
	/// </summary>
	[AllowNull]
	[Category("TLS")]
	[DisplayName("Certificate Thumbprint")]
	[DefaultValue("")]
	[Description("Specifies which certificate should be used from the certificate store specified in Certificate Store Location")]
	public string CertificateThumbprint
	{
		get => SingleStoreConnectionStringOption.CertificateThumbprint.GetValue(this);
		set => SingleStoreConnectionStringOption.CertificateThumbprint.SetValue(this, value);
	}

	/// <summary>
	/// The path to the client’s SSL certificate file in PEM format. <see cref="SslKey"/> must also be specified, and <see cref="CertificateFile"/> should not be.
	/// </summary>
	[AllowNull]
	[Category("TLS")]
	[DefaultValue("")]
	[Description("The path to the client’s SSL certificate file in PEM format.")]
	[DisplayName("SSL Cert")]
	public string SslCert
	{
		get => SingleStoreConnectionStringOption.SslCert.GetValue(this);
		set => SingleStoreConnectionStringOption.SslCert.SetValue(this, value);
	}

	/// <summary>
	/// The path to the client’s SSL private key in PEM format. <see cref="SslCert"/> must also be specified, and <see cref="CertificateFile"/> should not be.
	/// </summary>
	[AllowNull]
	[Category("TLS")]
	[DefaultValue("")]
	[Description("The path to the client’s SSL private key in PEM format.")]
	[DisplayName("SSL Key")]
	public string SslKey
	{
		get => SingleStoreConnectionStringOption.SslKey.GetValue(this);
		set => SingleStoreConnectionStringOption.SslKey.SetValue(this, value);
	}

	/// <summary>
	/// Use <see cref="SslCa"/> instead.
	/// </summary>
	[AllowNull]
	[Browsable(false)]
	[Category("Obsolete")]
	[DisplayName("CA Certificate File")]
	[Obsolete("Use SslCa instead.")]
	public string CACertificateFile
	{
		get => SingleStoreConnectionStringOption.SslCa.GetValue(this);
		set => SingleStoreConnectionStringOption.SslCa.SetValue(this, value);
	}

	/// <summary>
	/// The path to a CA certificate file in a PEM Encoded (.pem) format. This should be used with a value for the <see cref="SslMode"/> property of <see cref="SingleStoreSslMode.VerifyCA"/> or <see cref="SingleStoreSslMode.VerifyFull"/> to enable verification of a CA certificate that is not trusted by the operating system’s certificate store.
	/// </summary>
	[AllowNull]
	[Category("TLS")]
	[DefaultValue("")]
	[Description("The path to a CA certificate file in a PEM Encoded (.pem) format.")]
	[DisplayName("SSL CA")]
	public string SslCa
	{
		get => SingleStoreConnectionStringOption.SslCa.GetValue(this);
		set => SingleStoreConnectionStringOption.SslCa.SetValue(this, value);
	}

	/// <summary>
	/// The TLS versions which may be used during TLS negotiation, or empty to use OS defaults.
	/// </summary>
	[AllowNull]
	[Category("TLS")]
	[DisplayName("TLS Version")]
	[DefaultValue("")]
	[Description("The TLS versions which may be used during TLS negotiation.")]
	public string TlsVersion
	{
		get => SingleStoreConnectionStringOption.TlsVersion.GetValue(this);
		set => SingleStoreConnectionStringOption.TlsVersion.SetValue(this, value);
	}

	/// <summary>
	/// The TLS cipher suites which may be used during TLS negotiation. The default value (the empty string) allows the OS to determine the TLS cipher suites to use; this is the recommended setting.
	/// </summary>
	[AllowNull]
	[Category("TLS")]
	[DefaultValue("")]
	[Description("The TLS cipher suites which may be used during TLS negotiation.")]
	[DisplayName("TLS Cipher Suites")]
	public string TlsCipherSuites
	{
		get => SingleStoreConnectionStringOption.TlsCipherSuites.GetValue(this);
		set => SingleStoreConnectionStringOption.TlsCipherSuites.SetValue(this, value);
	}

	// Connection Pooling Options

	/// <summary>
	/// Enables connection pooling.
	/// </summary>
	[Category("Pooling")]
	[DefaultValue(true)]
	[Description("Enables connection pooling.")]
	[DisplayName("Pooling")]
	public bool Pooling
	{
		get => SingleStoreConnectionStringOption.Pooling.GetValue(this);
		set => SingleStoreConnectionStringOption.Pooling.SetValue(this, value);
	}

	/// <summary>
	/// The maximum lifetime (in seconds) for any connection, or <c>0</c> for no lifetime limit.
	/// </summary>
	[Category("Pooling")]
	[DefaultValue(0u)]
	[Description("The maximum lifetime (in seconds) for any connection, or 0 for no lifetime limit.")]
	[DisplayName("Connection Lifetime")]
	public uint ConnectionLifeTime
	{
		get => SingleStoreConnectionStringOption.ConnectionLifeTime.GetValue(this);
		set => SingleStoreConnectionStringOption.ConnectionLifeTime.SetValue(this, value);
	}

	/// <summary>
	/// Whether connections are reset when being retrieved from the pool.
	/// </summary>
	[Category("Pooling")]
	[DefaultValue(true)]
	[Description("Whether connections are reset when being retrieved from the pool.")]
	[DisplayName("Connection Reset")]
	public bool ConnectionReset
	{
		get => SingleStoreConnectionStringOption.ConnectionReset.GetValue(this);
		set => SingleStoreConnectionStringOption.ConnectionReset.SetValue(this, value);
	}

	/// <summary>
	/// This option is no longer supported.
	/// </summary>
	[Category("Obsolete")]
	[DefaultValue(true)]
	[DisplayName("Defer Connection Reset")]
	[Obsolete("This option is no longer supported in MySqlConnector >= 1.4.0.")]
	public bool DeferConnectionReset
	{
		get => SingleStoreConnectionStringOption.DeferConnectionReset.GetValue(this);
		set => SingleStoreConnectionStringOption.DeferConnectionReset.SetValue(this, value);
	}

	/// <summary>
	/// This option is no longer supported.
	/// </summary>
	[Category("Obsolete")]
	[DefaultValue(0u)]
	[DisplayName("Connection Idle Ping Time")]
	[Obsolete("This option is no longer supported in SingleStoreConnector >= 1.4.0.")]
	public uint ConnectionIdlePingTime
	{
		get => SingleStoreConnectionStringOption.ConnectionIdlePingTime.GetValue(this);
		set => SingleStoreConnectionStringOption.ConnectionIdlePingTime.SetValue(this, value);
	}

	/// <summary>
	/// The amount of time (in seconds) that a connection can remain idle in the pool.
	/// </summary>
	[Category("Pooling")]
	[DefaultValue(180u)]
	[Description("The amount of time (in seconds) that a connection can remain idle in the pool.")]
	[DisplayName("Connection Idle Timeout")]
	public uint ConnectionIdleTimeout
	{
		get => SingleStoreConnectionStringOption.ConnectionIdleTimeout.GetValue(this);
		set => SingleStoreConnectionStringOption.ConnectionIdleTimeout.SetValue(this, value);
	}

	/// <summary>
	/// The minimum number of connections to leave in the pool if <see cref="ConnectionIdleTimeout"/> is reached.
	/// </summary>
	[Category("Pooling")]
	[DefaultValue(0u)]
	[Description("The minimum number of connections to leave in the pool if Connection Idle Timeout is reached.")]
	[DisplayName("Minimum Pool Size")]
	public uint MinimumPoolSize
	{
		get => SingleStoreConnectionStringOption.MinimumPoolSize.GetValue(this);
		set => SingleStoreConnectionStringOption.MinimumPoolSize.SetValue(this, value);
	}

	/// <summary>
	/// The maximum number of connections allowed in the pool.
	/// </summary>
	[Category("Pooling")]
	[DefaultValue(100u)]
	[Description("The maximum number of connections allowed in the pool.")]
	[DisplayName("Maximum Pool Size")]
	public uint MaximumPoolSize
	{
		get => SingleStoreConnectionStringOption.MaximumPoolSize.GetValue(this);
		set => SingleStoreConnectionStringOption.MaximumPoolSize.SetValue(this, value);
	}

	/// <summary>
	/// The number of seconds between checks for DNS changes, or 0 to disable periodic checks.
	/// </summary>
	[Category("Pooling")]
	[DefaultValue(0u)]
	[Description("The number of seconds between checks for DNS changes.")]
	[DisplayName("DNS Check Interval")]
	public uint DnsCheckInterval
	{
		get => SingleStoreConnectionStringOption.DnsCheckInterval.GetValue(this);
		set => SingleStoreConnectionStringOption.DnsCheckInterval.SetValue(this, value);
	}

	// Other Options

	/// <summary>
	/// Allows the <c>LOAD DATA LOCAL</c> command to request files from the client.
	/// </summary>
	[Category("Other")]
	[DefaultValue(false)]
	[Description("Allows the LOAD DATA LOCAL command to request files from the client.")]
	[DisplayName("Allow Load Local Infile")]
	public bool AllowLoadLocalInfile
	{
		get => SingleStoreConnectionStringOption.AllowLoadLocalInfile.GetValue(this);
		set => SingleStoreConnectionStringOption.AllowLoadLocalInfile.SetValue(this, value);
	}

	/// <summary>
	/// Allows the client to automatically request the RSA public key from the server.
	/// </summary>
	[Category("Other")]
	[DefaultValue(false)]
	[Description("Allows the client to automatically request the RSA public key from the server.")]
	[DisplayName("Allow Public Key Retrieval")]
	public bool AllowPublicKeyRetrieval
	{
		get => SingleStoreConnectionStringOption.AllowPublicKeyRetrieval.GetValue(this);
		set => SingleStoreConnectionStringOption.AllowPublicKeyRetrieval.SetValue(this, value);
	}

	/// <summary>
	/// Allows user-defined variables (prefixed with <c>@</c>) to be used in SQL statements.
	/// </summary>
	[Category("Other")]
	[DefaultValue(false)]
	[Description("Allows user-defined variables (prefixed with @) to be used in SQL statements.")]
	[DisplayName("Allow User Variables")]
	public bool AllowUserVariables
	{
		get => SingleStoreConnectionStringOption.AllowUserVariables.GetValue(this);
		set => SingleStoreConnectionStringOption.AllowUserVariables.SetValue(this, value);
	}

	/// <summary>
	/// Returns <c>DATETIME</c> fields as <see cref="SingleStoreDateTime"/> objects instead of <see cref="DateTime"/> objects.
	/// </summary>
	[Category("Other")]
	[DefaultValue(false)]
	[Description("Returns DATETIME fields as SingleStoreDateTime objects instead of DateTime objects.")]
	[DisplayName("Allow Zero DateTime")]
	public bool AllowZeroDateTime
	{
		get => SingleStoreConnectionStringOption.AllowZeroDateTime.GetValue(this);
		set => SingleStoreConnectionStringOption.AllowZeroDateTime.SetValue(this, value);
	}

	/// <summary>
	/// Sets the <c>program_name</c> connection attribute passed to SingleStore Server.
	/// </summary>
	[AllowNull]
	[Category("Other")]
	[DefaultValue("")]
	[Description("Sets the program_name connection attribute passed to SingleStore Server.")]
	[DisplayName("Application Name")]
	public string ApplicationName
	{
		get => SingleStoreConnectionStringOption.ApplicationName.GetValue(this);
		set => SingleStoreConnectionStringOption.ApplicationName.SetValue(this, value);
	}

	/// <summary>
	/// Sets connection attributes passed to SingleStore Server.
	/// </summary>
	[AllowNull]
	[Category("Other")]
	[DefaultValue("")]
	[Description("Sets connection attributes passed to SingleStore Server.")]
	[DisplayName("Connection Attributes")]
	public string ConnectionAttributes
	{
		get => SingleStoreConnectionStringOption.ConnectionAttributes.GetValue(this);
		set => SingleStoreConnectionStringOption.ConnectionAttributes.SetValue(this, value);
	}

	/// <summary>
	/// Automatically enlists this connection in any active <see cref="System.Transactions.TransactionScope"/>.
	/// </summary>
	[Category("Other")]
	[DefaultValue(true)]
	[Description("Automatically enlists this connection in any active TransactionScope.")]
	[DisplayName("Auto Enlist")]
	public bool AutoEnlist
	{
		get => SingleStoreConnectionStringOption.AutoEnlist.GetValue(this);
		set => SingleStoreConnectionStringOption.AutoEnlist.SetValue(this, value);
	}

	/// <summary>
	/// The length of time (in seconds) to wait for a query to be canceled when <see cref="SingleStoreCommand.CommandTimeout"/> expires, or zero for no timeout.
	/// </summary>
	[Category("Other")]
	[DefaultValue(2)]
	[Description("The length of time (in seconds) to wait for a query to be canceled when MySqlCommand.CommandTimeout expires, or zero for no timeout.")]
	[DisplayName("Cancellation Timeout")]
	public int CancellationTimeout
	{
		get => SingleStoreConnectionStringOption.CancellationTimeout.GetValue(this);
		set => SingleStoreConnectionStringOption.CancellationTimeout.SetValue(this, value);
	}

	/// <summary>
	/// Supported for backwards compatibility; SingleStoreConnector always uses <c>utf8mb4</c>.
	/// </summary>
	[AllowNull]
	[Category("Obsolete")]
	[DefaultValue("")]
	[DisplayName("Character Set")]
	public string CharacterSet
	{
		get => SingleStoreConnectionStringOption.CharacterSet.GetValue(this);
		set => SingleStoreConnectionStringOption.CharacterSet.SetValue(this, value);
	}

	/// <summary>
	/// The length of time (in seconds) to wait for a connection to the server before terminating the attempt and generating an error.
	/// The default value is 15.
	/// </summary>
	[Category("Connection")]
	[Description("The length of time (in seconds) to wait for a connection to the server before terminating the attempt and generating an error.")]
	[DefaultValue(15u)]
	[DisplayName("Connection Timeout")]
	public uint ConnectionTimeout
	{
		get => SingleStoreConnectionStringOption.ConnectionTimeout.GetValue(this);
		set => SingleStoreConnectionStringOption.ConnectionTimeout.SetValue(this, value);
	}

	/// <summary>
	/// Whether invalid <c>DATETIME</c> fields should be converted to <see cref="DateTime.MinValue"/>.
	/// </summary>
	[Category("Other")]
	[DefaultValue(true)]
	[Description("Whether invalid DATETIME fields should be converted to DateTime.MinValue.")]
	[DisplayName("Convert Zero DateTime")]
	public bool ConvertZeroDateTime
	{
		get => SingleStoreConnectionStringOption.ConvertZeroDateTime.GetValue(this);
		set => SingleStoreConnectionStringOption.ConvertZeroDateTime.SetValue(this, value);
	}

	/// <summary>
	/// The <see cref="DateTimeKind"/> to use when deserializing <c>DATETIME</c> values.
	/// </summary>
	[Category("Other")]
	[DefaultValue(SingleStoreDateTimeKind.Unspecified)]
	[Description("The DateTimeKind to use when deserializing DATETIME values.")]
	[DisplayName("DateTime Kind")]
	public SingleStoreDateTimeKind DateTimeKind
	{
		get => SingleStoreConnectionStringOption.DateTimeKind.GetValue(this);
		set => SingleStoreConnectionStringOption.DateTimeKind.SetValue(this, value);
	}

	/// <summary>
	/// The length of time (in seconds) each command can execute before the query is cancelled on the server, or zero to disable timeouts.
	/// </summary>
	[Category("Other")]
	[DefaultValue(30u)]
	[Description("The length of time (in seconds) each command can execute before the query is cancelled on the server, or zero to disable timeouts.")]
	[DisplayName("Default Command Timeout")]
	public uint DefaultCommandTimeout
	{
		get => SingleStoreConnectionStringOption.DefaultCommandTimeout.GetValue(this);
		set => SingleStoreConnectionStringOption.DefaultCommandTimeout.SetValue(this, value);
	}

	/// <summary>
	/// Forces all async methods to execute synchronously. This can be useful for debugging.
	/// </summary>
	[Category("Other")]
	[DefaultValue(false)]
	[Description("Forces all async methods to execute synchronously.")]
	[DisplayName("Force Synchronous")]
	public bool ForceSynchronous
	{
		get => SingleStoreConnectionStringOption.ForceSynchronous.GetValue(this);
		set => SingleStoreConnectionStringOption.ForceSynchronous.SetValue(this, value);
	}

	/// <summary>
	/// Determines whether CHAR(48) should be read as a GeographyPoint.
	/// </summary>
	[Category("Other")]
	[DefaultValue(false)]
	[Description("Determines whether CHAR(48) should be read as a GeographyPoint.")]
	[DisplayName("TreatChar48AsGeographyPoint")]
	public bool TreatChar48AsGeographyPoint
	{
		get => SingleStoreConnectionStringOption.TreatChar48AsGeographyPoint.GetValue(this);
		set => SingleStoreConnectionStringOption.TreatChar48AsGeographyPoint.SetValue(this, value);
	}

	/// <summary>
	/// Determines which column type (if any) should be read as a <see cref="Guid"/>.
	/// </summary>
	[Category("Other")]
	[DefaultValue(SingleStoreGuidFormat.Default)]
	[Description("Determines which column type (if any) should be read as a Guid.")]
	[DisplayName("GUID Format")]
	public SingleStoreGuidFormat GuidFormat
	{
		get => SingleStoreConnectionStringOption.GuidFormat.GetValue(this);
		set => SingleStoreConnectionStringOption.GuidFormat.SetValue(this, value);
	}

	/// <summary>
	/// Does not check the <see cref="SingleStoreCommand.Transaction"/> property for validity when executing a command.
	/// </summary>
	[Category("Other")]
	[DefaultValue(false)]
	[Description("Does not check the SingleStoreCommand.Transaction property for validity when executing a command.")]
	[DisplayName("Ignore Command Transaction")]
	public bool IgnoreCommandTransaction
	{
		get => SingleStoreConnectionStringOption.IgnoreCommandTransaction.GetValue(this);
		set => SingleStoreConnectionStringOption.IgnoreCommandTransaction.SetValue(this, value);
	}

	/// <summary>
	/// Ignores calls to <see cref="SingleStoreCommand.Prepare"/> and <c>PrepareAsync</c>.
	/// </summary>
	[Category("Other")]
	[DefaultValue(false)]
	[Description("Ignores calls to SingleStoreCommand.Prepare and PrepareAsync.")]
	[DisplayName("Ignore Prepare")]
	public bool IgnorePrepare
	{
		get => SingleStoreConnectionStringOption.IgnorePrepare.GetValue(this);
		set => SingleStoreConnectionStringOption.IgnorePrepare.SetValue(this, value);
	}

	/// <summary>
	/// Instructs the SingleStore server that this is an interactive session.
	/// </summary>
	[Category("Connection")]
	[DefaultValue(false)]
	[Description("Instructs the SingleStore server that this is an interactive session.")]
	[DisplayName("Interactive Session")]
	public bool InteractiveSession
	{
		get => SingleStoreConnectionStringOption.InteractiveSession.GetValue(this);
		set => SingleStoreConnectionStringOption.InteractiveSession.SetValue(this, value);
	}

	/// <summary>
	/// TCP Keepalive idle time (in seconds), or 0 to use OS defaults.
	/// </summary>
	[Category("Connection")]
	[DefaultValue(0u)]
	[Description("TCP Keepalive idle time (in seconds), or 0 to use OS defaults.")]
	[DisplayName("Keep Alive")]
	public uint Keepalive
	{
		get => SingleStoreConnectionStringOption.Keepalive.GetValue(this);
		set => SingleStoreConnectionStringOption.Keepalive.SetValue(this, value);
	}

	/// <summary>
	/// Doesn't escape backslashes in string parameters. For use with the <c>NO_BACKSLASH_ESCAPES</c> SingleStore server mode.
	/// </summary>
	[Category("Other")]
	[DefaultValue(false)]
	[Description("Doesn't escape backslashes in string parameters. For use with the NO_BACKSLASH_ESCAPES SingleStore server mode.")]
	[DisplayName("No Backslash Escapes")]
	public bool NoBackslashEscapes
	{
		get => SingleStoreConnectionStringOption.NoBackslashEscapes.GetValue(this);
		set => SingleStoreConnectionStringOption.NoBackslashEscapes.SetValue(this, value);
	}

	/// <summary>
	/// Use the <see cref="GuidFormat"/> property instead.
	/// </summary>
	[Category("Obsolete")]
	[DisplayName("Old Guids")]
	[DefaultValue(false)]
	public bool OldGuids
	{
		get => SingleStoreConnectionStringOption.OldGuids.GetValue(this);
		set => SingleStoreConnectionStringOption.OldGuids.SetValue(this, value);
	}

	/// <summary>
	/// If true, preserves security-sensitive information in the connection string retrieved from any open <see cref="SingleStoreConnection"/>.
	/// </summary>
	[Category("Other")]
	[DisplayName("Persist Security Info")]
	[DefaultValue(false)]
	[Description("Preserves security-sensitive information in the connection string retrieved from any open SingleStoreConnection.")]
	public bool PersistSecurityInfo
	{
		get => SingleStoreConnectionStringOption.PersistSecurityInfo.GetValue(this);
		set => SingleStoreConnectionStringOption.PersistSecurityInfo.SetValue(this, value);
	}

	/// <summary>
	/// Enables query pipelining.
	/// </summary>
	[Category("Other")]
	[DefaultValue(true)]
	[Description("Enables query pipelining.")]
	[DisplayName("Pipelining")]
	public bool Pipelining
	{
		get => SingleStoreConnectionStringOption.Pipelining.GetValue(this);
		set => SingleStoreConnectionStringOption.Pipelining.SetValue(this, value);
	}

	/// <summary>
		/// Whether to use server redirection.
		/// </summary>
	[Category("Connection")]
	[DefaultValue(SingleStoreServerRedirectionMode.Disabled)]
	[Description("Whether to use server redirection.")]
	[DisplayName("Server Redirection Mode")]
	public SingleStoreServerRedirectionMode ServerRedirectionMode
	{
		get => SingleStoreConnectionStringOption.ServerRedirectionMode.GetValue(this);
		set => SingleStoreConnectionStringOption.ServerRedirectionMode.SetValue(this, value);
	}

	/// <summary>
	/// The path to a file containing the server's RSA public key.
	/// </summary>
	[AllowNull]
	[Category("Connection")]
	[DisplayName("Server RSA Public Key File")]
	[DefaultValue("")]
	[Description("The path to a file containing the server's RSA public key.")]
	public string ServerRsaPublicKeyFile
	{
		get => SingleStoreConnectionStringOption.ServerRsaPublicKeyFile.GetValue(this);
		set => SingleStoreConnectionStringOption.ServerRsaPublicKeyFile.SetValue(this, value);
	}

	/// <summary>
	/// The server’s Service Principal Name (for <c>auth_gssapi_client</c> authentication).
	/// </summary>
	[AllowNull]
	[Category("Connection")]
	[DefaultValue("")]
	[Description("The server’s Service Principal Name (for auth_gssapi_client authentication).")]
	[DisplayName("Server SPN")]
	public string ServerSPN
	{
		get => SingleStoreConnectionStringOption.ServerSPN.GetValue(this);
		set => SingleStoreConnectionStringOption.ServerSPN.SetValue(this, value);
	}

	/// <summary>
	/// Returns <c>TINYINT(1)</c> fields as <see cref="bool"/> values.
	/// </summary>
	[Category("Other")]
	[DisplayName("Treat Tiny As Boolean")]
	[DefaultValue(true)]
	[Description("Returns TINYINT(1) fields as Boolean values.")]
	public bool TreatTinyAsBoolean
	{
		get => SingleStoreConnectionStringOption.TreatTinyAsBoolean.GetValue(this);
		set => SingleStoreConnectionStringOption.TreatTinyAsBoolean.SetValue(this, value);
	}

	/// <summary>
	/// Report changed rows instead of found rows.
	/// </summary>
	[Category("Other")]
	[DefaultValue(false)]
	[Description("Report changed rows instead of found rows.")]
	[DisplayName("Use Affected Rows")]
	public bool UseAffectedRows
	{
		get => SingleStoreConnectionStringOption.UseAffectedRows.GetValue(this);
		set => SingleStoreConnectionStringOption.UseAffectedRows.SetValue(this, value);
	}

	/// <summary>
	/// Compress packets sent to and from the server.
	/// </summary>
	[Category("Other")]
	[DefaultValue(false)]
	[Description("Compress packets sent to and from the server.")]
	[DisplayName("Use Compression")]
	public bool UseCompression
	{
		get => SingleStoreConnectionStringOption.UseCompression.GetValue(this);
		set => SingleStoreConnectionStringOption.UseCompression.SetValue(this, value);
	}

	/// <summary>
	/// Use XA transactions to implement <see cref="System.Transactions.TransactionScope"/> distributed transactions.
	/// </summary>
	[Category("Other")]
	[DefaultValue(true)]
	[Description("Use XA transactions to implement System.Transactions distributed transactions.")]
	[DisplayName("Use XA Transactions")]
	public bool UseXaTransactions
	{
		get => SingleStoreConnectionStringOption.UseXaTransactions.GetValue(this);
		set => SingleStoreConnectionStringOption.UseXaTransactions.SetValue(this, value);
	}

	// Other Methods

	/// <summary>
	/// Returns an <see cref="ICollection"/> that contains the keys in the <see cref="SingleStoreConnectionStringBuilder"/>.
	/// </summary>
	public override ICollection Keys => base.Keys.Cast<string>().OrderBy(x => SingleStoreConnectionStringOption.OptionNames.IndexOf(x)).ToList();

	/// <summary>
	/// Whether this <see cref="SingleStoreConnectionStringBuilder"/> contains a set option with the specified name.
	/// </summary>
	/// <param name="keyword">The option name.</param>
	/// <returns><c>true</c> if an option with that name is set; otherwise, <c>false</c>.</returns>
	public override bool ContainsKey(string keyword)
	{
		var option = SingleStoreConnectionStringOption.TryGetOptionForKey(keyword);
		return option is object && base.ContainsKey(option.Key);
	}

	/// <summary>
	/// Removes the option with the specified name.
	/// </summary>
	/// <param name="keyword">The option name.</param>
	public override bool Remove(string keyword)
	{
		var option = SingleStoreConnectionStringOption.TryGetOptionForKey(keyword);
		return option is object && base.Remove(option.Key);
	}

	/// <summary>
	/// Retrieves an option value by name.
	/// </summary>
	/// <param name="key">The option name.</param>
	/// <returns>That option's value, if set.</returns>
	[AllowNull]
	public override object this[string key]
	{
		get => SingleStoreConnectionStringOption.GetOptionForKey(key).GetObject(this);
		set
		{
			var option = SingleStoreConnectionStringOption.GetOptionForKey(key);
			if (value is null)
				base[option.Key] = null;
			else
				option.SetObject(this, value);
		}
	}

	internal void DoSetValue(string key, object? value) => base[key] = value;

	internal string GetConnectionString(bool includePassword)
	{
		var connectionString = ConnectionString;
		if (includePassword)
			return connectionString;

		if (m_cachedConnectionString != connectionString)
		{
			var csb = new SingleStoreConnectionStringBuilder(connectionString);
			foreach (string? key in Keys)
			{
				foreach (var passwordKey in SingleStoreConnectionStringOption.Password.Keys)
				{
					if (string.Equals(key, passwordKey, StringComparison.OrdinalIgnoreCase))
						csb.Remove(key!);
				}
			}
			m_cachedConnectionStringWithoutPassword = csb.ConnectionString;
			m_cachedConnectionString = connectionString;
		}

		return m_cachedConnectionStringWithoutPassword!;
	}

	/// <summary>
	/// Fills in <paramref name="propertyDescriptors"/> with information about the available properties on this object.
	/// </summary>
	/// <param name="propertyDescriptors">The collection of <see cref="PropertyDescriptor"/> objects to populate.</param>
	protected override void GetProperties(Hashtable propertyDescriptors)
	{
		base.GetProperties(propertyDescriptors);

		// only report properties with a [Category] attribute that are not [Obsolete]
		var propertiesToRemove = propertyDescriptors.Values
			.Cast<PropertyDescriptor>()
			.Where(static x => !x.Attributes.OfType<CategoryAttribute>().Any() || x.Attributes.OfType<ObsoleteAttribute>().Any())
			.ToList();
		foreach (var property in propertiesToRemove)
			propertyDescriptors.Remove(property.DisplayName);
	}

	private string? m_cachedConnectionString;
	private string? m_cachedConnectionStringWithoutPassword;
}

internal abstract partial class SingleStoreConnectionStringOption
{
	public static List<string> OptionNames { get; } = new();

	// Connection Options
	public static readonly SingleStoreConnectionStringReferenceOption<string> Server;
	public static readonly SingleStoreConnectionStringValueOption<uint> Port;
	public static readonly SingleStoreConnectionStringReferenceOption<string> UserID;
	public static readonly SingleStoreConnectionStringReferenceOption<string> Password;
	public static readonly SingleStoreConnectionStringReferenceOption<string> Database;
	public static readonly SingleStoreConnectionStringValueOption<SingleStoreLoadBalance> LoadBalance;
	public static readonly SingleStoreConnectionStringValueOption<SingleStoreConnectionProtocol> ConnectionProtocol;
	public static readonly SingleStoreConnectionStringReferenceOption<string> PipeName;

	// SSL/TLS Options
	public static readonly SingleStoreConnectionStringValueOption<SingleStoreSslMode> SslMode;
	public static readonly SingleStoreConnectionStringReferenceOption<string> CertificateFile;
	public static readonly SingleStoreConnectionStringReferenceOption<string> CertificatePassword;
	public static readonly SingleStoreConnectionStringValueOption<SingleStoreCertificateStoreLocation> CertificateStoreLocation;
	public static readonly SingleStoreConnectionStringReferenceOption<string> CertificateThumbprint;
	public static readonly SingleStoreConnectionStringReferenceOption<string> SslCert;
	public static readonly SingleStoreConnectionStringReferenceOption<string> SslKey;
	public static readonly SingleStoreConnectionStringReferenceOption<string> SslCa;
	public static readonly SingleStoreConnectionStringReferenceOption<string> TlsVersion;
	public static readonly SingleStoreConnectionStringReferenceOption<string> TlsCipherSuites;

	// Connection Pooling Options
	public static readonly SingleStoreConnectionStringValueOption<bool> Pooling;
	public static readonly SingleStoreConnectionStringValueOption<uint> ConnectionLifeTime;
	public static readonly SingleStoreConnectionStringValueOption<bool> ConnectionReset;
	public static readonly SingleStoreConnectionStringValueOption<bool> DeferConnectionReset;
	public static readonly SingleStoreConnectionStringValueOption<uint> ConnectionIdlePingTime;
	public static readonly SingleStoreConnectionStringValueOption<uint> ConnectionIdleTimeout;
	public static readonly SingleStoreConnectionStringValueOption<uint> MinimumPoolSize;
	public static readonly SingleStoreConnectionStringValueOption<uint> MaximumPoolSize;
	public static readonly SingleStoreConnectionStringValueOption<uint> DnsCheckInterval;

	// Other Options
	public static readonly SingleStoreConnectionStringValueOption<bool> AllowLoadLocalInfile;
	public static readonly SingleStoreConnectionStringValueOption<bool> AllowPublicKeyRetrieval;
	public static readonly SingleStoreConnectionStringValueOption<bool> AllowUserVariables;
	public static readonly SingleStoreConnectionStringValueOption<bool> AllowZeroDateTime;
	public static readonly SingleStoreConnectionStringReferenceOption<string> ApplicationName;
	public static readonly SingleStoreConnectionStringValueOption<bool> AutoEnlist;
	public static readonly SingleStoreConnectionStringValueOption<int> CancellationTimeout;
	public static readonly SingleStoreConnectionStringReferenceOption<string> CharacterSet;
	public static readonly SingleStoreConnectionStringReferenceOption<string> ConnectionAttributes;
	public static readonly SingleStoreConnectionStringValueOption<uint> ConnectionTimeout;
	public static readonly SingleStoreConnectionStringValueOption<bool> ConvertZeroDateTime;
	public static readonly SingleStoreConnectionStringValueOption<SingleStoreDateTimeKind> DateTimeKind;
	public static readonly SingleStoreConnectionStringValueOption<uint> DefaultCommandTimeout;
	public static readonly SingleStoreConnectionStringValueOption<bool> ForceSynchronous;
	public static readonly SingleStoreConnectionStringValueOption<bool> TreatChar48AsGeographyPoint;
	public static readonly SingleStoreConnectionStringValueOption<SingleStoreGuidFormat> GuidFormat;
	public static readonly SingleStoreConnectionStringValueOption<bool> IgnoreCommandTransaction;
	public static readonly SingleStoreConnectionStringValueOption<bool> IgnorePrepare;
	public static readonly SingleStoreConnectionStringValueOption<bool> InteractiveSession;
	public static readonly SingleStoreConnectionStringValueOption<uint> Keepalive;
	public static readonly SingleStoreConnectionStringValueOption<bool> NoBackslashEscapes;
	public static readonly SingleStoreConnectionStringValueOption<bool> OldGuids;
	public static readonly SingleStoreConnectionStringValueOption<bool> PersistSecurityInfo;
	public static readonly SingleStoreConnectionStringValueOption<bool> Pipelining;
	public static readonly SingleStoreConnectionStringValueOption<SingleStoreServerRedirectionMode> ServerRedirectionMode;
	public static readonly SingleStoreConnectionStringReferenceOption<string> ServerRsaPublicKeyFile;
	public static readonly SingleStoreConnectionStringReferenceOption<string> ServerSPN;
	public static readonly SingleStoreConnectionStringValueOption<bool> TreatTinyAsBoolean;
	public static readonly SingleStoreConnectionStringValueOption<bool> UseAffectedRows;
	public static readonly SingleStoreConnectionStringValueOption<bool> UseCompression;
	public static readonly SingleStoreConnectionStringValueOption<bool> UseXaTransactions;

	public static SingleStoreConnectionStringOption? TryGetOptionForKey(string key) =>
		s_options.TryGetValue(key, out var option) ? option : null;

	public static SingleStoreConnectionStringOption GetOptionForKey(string key) =>
		TryGetOptionForKey(key) ?? throw new ArgumentException("Option '{0}' not supported.".FormatInvariant(key));

	public string Key => m_keys[0];
	public IReadOnlyList<string> Keys => m_keys;

	public abstract object GetObject(SingleStoreConnectionStringBuilder builder);
	public abstract void SetObject(SingleStoreConnectionStringBuilder builder, object value);

	protected SingleStoreConnectionStringOption(IReadOnlyList<string> keys)
	{
		m_keys = keys;
	}

	private static void AddOption(SingleStoreConnectionStringOption option)
	{
		foreach (string key in option.m_keys)
			s_options.Add(key, option);
		OptionNames.Add(option.m_keys[0]);
	}

#pragma warning disable CA1065 // Do not raise exceptions in unexpected locations
#pragma warning disable CA1810 // Initialize reference type static fields inline
	static SingleStoreConnectionStringOption()
	{
		s_options = new(StringComparer.OrdinalIgnoreCase);

		// Base Options
		AddOption(Server = new(
			keys: new[] { "Server", "Host", "Data Source", "DataSource", "Address", "Addr", "Network Address" },
			defaultValue: ""));

		AddOption(Port = new(
			keys: new[] { "Port" },
			defaultValue: 3306u));

		AddOption(UserID = new(
			keys: new[] { "User ID", "UserID", "Username", "Uid", "User name", "User" },
			defaultValue: ""));

		AddOption(Password = new(
			keys: new[] { "Password", "pwd" },
			defaultValue: ""));

		AddOption(Database = new(
			keys: new[] { "Database", "Initial Catalog" },
			defaultValue: ""));

		AddOption(LoadBalance = new(
			keys: new[] { "Load Balance", "LoadBalance" },
			defaultValue: SingleStoreLoadBalance.RoundRobin));

		AddOption(ConnectionProtocol = new(
			keys: new[] { "Connection Protocol", "ConnectionProtocol", "Protocol" },
			defaultValue: SingleStoreConnectionProtocol.Socket));

		AddOption(PipeName = new(
			keys: new[] { "Pipe Name", "PipeName", "Pipe" },
			defaultValue: "MYSQL"));

		// SSL/TLS Options
		AddOption(SslMode = new(
			keys: new[] { "SSL Mode", "SslMode" },
			defaultValue: SingleStoreSslMode.Preferred));

		AddOption(CertificateFile = new(
			keys: new[] { "Certificate File", "CertificateFile" },
			defaultValue: ""));

		AddOption(CertificatePassword = new(
			keys: new[] { "Certificate Password", "CertificatePassword" },
			defaultValue: ""));

		AddOption(CertificateStoreLocation = new(
			keys: new[] { "Certificate Store Location", "CertificateStoreLocation" },
			defaultValue: SingleStoreCertificateStoreLocation.None));

		AddOption(CertificateThumbprint = new(
			keys: new[] { "Certificate Thumbprint", "CertificateThumbprint", "Certificate Thumb Print" },
			defaultValue: ""));

		AddOption(SslCert = new(
			keys: new[] { "SSL Cert", "SslCert", "Ssl-Cert" },
			defaultValue: ""));

		AddOption(SslKey = new(
			keys: new[] { "SSL Key", "SslKey", "Ssl-Key" },
			defaultValue: ""));

		AddOption(SslCa = new(
			keys: new[] { "SSL CA", "CACertificateFile", "CA Certificate File", "SslCa", "Ssl-Ca" },
			defaultValue: ""));

		AddOption(TlsVersion = new(
			keys: new[] { "TLS Version", "TlsVersion", "Tls-Version" },
			defaultValue: "",
			coerce: value =>
			{
				if (string.IsNullOrWhiteSpace(value))
					return "";

				Span<bool> versions = stackalloc bool[4];
				foreach (var part in value!.TrimStart('[', '(').TrimEnd(')', ']').Split(','))
				{
					var match = TlsVersionsRegex().Match(part);
					if (!match.Success)
						throw new ArgumentException($"Unrecognized TlsVersion protocol version '{part}'; permitted versions are: TLS 1.0, TLS 1.1, TLS 1.2, TLS 1.3.");
					var version = match.Groups[2].Value;
					if (version is "" or "1" or "10" or "1.0")
						versions[0] = true;
					else if (version is "11" or "1.1")
						versions[1] = true;
					else if (version is "12" or "1.2")
						versions[2] = true;
					else if (version is "13" or "1.3")
						versions[3] = true;
				}

				var coercedValue = "";
				for (var i = 0; i < versions.Length; i++)
				{
					if (versions[i])
					{
						if (coercedValue.Length != 0)
							coercedValue += ", ";
						coercedValue += "TLS 1.{0}".FormatInvariant(i);
					}
				}
				return coercedValue;
			}));

		AddOption(TlsCipherSuites = new(
			keys: new[] { "TLS Cipher Suites", "TlsCipherSuites" },
			defaultValue: ""));

		// Connection Pooling Options
		AddOption(Pooling = new(
			keys: new[] { "Pooling" },
			defaultValue: true));

		AddOption(ConnectionLifeTime = new(
			keys: new[] { "Connection Lifetime", "ConnectionLifeTime" },
			defaultValue: 0u));

		AddOption(ConnectionReset = new(
			keys: new[] { "Connection Reset", "ConnectionReset" },
			defaultValue: true));

		AddOption(DeferConnectionReset = new(
			keys: new[] { "Defer Connection Reset", "DeferConnectionReset" },
			defaultValue: true));

		AddOption(ConnectionIdlePingTime = new(
			keys: new[] { "Connection Idle Ping Time", "ConnectionIdlePingTime" },
			defaultValue: 0u));

		AddOption(ConnectionIdleTimeout = new(
			keys: new[] { "Connection Idle Timeout", "ConnectionIdleTimeout" },
			defaultValue: 180u));

		AddOption(MinimumPoolSize = new(
			keys: new[] { "Minimum Pool Size", "Min Pool Size", "MinimumPoolSize", "minpoolsize" },
			defaultValue: 0u));

		AddOption(MaximumPoolSize = new(
			keys: new[] { "Maximum Pool Size", "Max Pool Size", "MaximumPoolSize", "maxpoolsize" },
			defaultValue: 100u));

		AddOption(DnsCheckInterval = new(
			keys: new[] { "DNS Check Interval", "DnsCheckInterval" },
			defaultValue: 0u));

		// Other Options
		AddOption(AllowLoadLocalInfile = new(
			keys: new[] { "Allow Load Local Infile", "AllowLoadLocalInfile" },
			defaultValue: false));

		AddOption(AllowPublicKeyRetrieval = new(
			keys: new[] { "Allow Public Key Retrieval", "AllowPublicKeyRetrieval" },
			defaultValue: false));

		AddOption(AllowUserVariables = new(
			keys: new[] { "Allow User Variables", "AllowUserVariables" },
			defaultValue: false));

		AddOption(AllowZeroDateTime = new(
			keys: new[] { "Allow Zero DateTime", "AllowZeroDateTime" },
			defaultValue: false));

		AddOption(ApplicationName = new(
			keys: new[] { "Application Name", "ApplicationName" },
			defaultValue: ""));

		AddOption(ConnectionAttributes = new(
			keys: new[] { "Connection Attributes", "ConnectionAttributes" },
			defaultValue: ""));

		AddOption(AutoEnlist = new(
			keys: new[] { "Auto Enlist", "AutoEnlist" },
			defaultValue: true));

		AddOption(CancellationTimeout = new(
			keys: new[] { "Cancellation Timeout", "CancellationTimeout" },
			defaultValue: 2,
			coerce: x =>
			{
				if (x < -1)
					throw new ArgumentOutOfRangeException(nameof(CancellationTimeout), "CancellationTimeout must be greater than or equal to -1");
				return x;
			}));

		AddOption(CharacterSet = new(
			keys: new[] { "Character Set", "CharSet", "CharacterSet" },
			defaultValue: ""));

		AddOption(ConnectionTimeout = new(
			keys: new[] { "Connection Timeout", "ConnectionTimeout", "Connect Timeout" },
			defaultValue: 15u));

		AddOption(ConvertZeroDateTime = new(
			keys: new[] { "Convert Zero DateTime", "ConvertZeroDateTime" },
			defaultValue: true));

		AddOption(DateTimeKind = new(
			keys: new[] { "DateTime Kind", "DateTimeKind" },
			defaultValue: SingleStoreDateTimeKind.Unspecified));

		AddOption(DefaultCommandTimeout = new(
			keys: new[] { "Default Command Timeout", "DefaultCommandTimeout", "Command Timeout" },
			defaultValue: 30u));

		AddOption(ForceSynchronous = new(
			keys: new[] { "Force Synchronous", "ForceSynchronous" },
			defaultValue: false));

		AddOption(TreatChar48AsGeographyPoint = new SingleStoreConnectionStringValueOption<bool>(
			new[] { "TreatChar48AsGeographyPoint", "Treat Char48 As GeographyPoint" },
			false));

		AddOption(GuidFormat = new(
			keys: new[] { "GUID Format", "GuidFormat" },
			defaultValue: SingleStoreGuidFormat.Default));

		AddOption(IgnoreCommandTransaction = new(
			keys: new[] { "Ignore Command Transaction", "IgnoreCommandTransaction" },
			defaultValue: false));

		AddOption(IgnorePrepare = new(
			keys: new[] { "Ignore Prepare", "IgnorePrepare" },
			defaultValue: false));

		AddOption(InteractiveSession = new(
			keys: new[] { "Interactive Session", "InteractiveSession", "Interactive" },
			defaultValue: false));

		AddOption(Keepalive = new(
			keys: new[] { "Keep Alive", "Keepalive" },
			defaultValue: 0u));

		AddOption(NoBackslashEscapes = new(
			keys: new[] { "No Backslash Escapes", "NoBackslashEscapes" },
			defaultValue: false));

		AddOption(OldGuids = new(
			keys: new[] { "Old Guids", "OldGuids" },
			defaultValue: false));

		AddOption(PersistSecurityInfo = new(
			keys: new[] { "Persist Security Info", "PersistSecurityInfo" },
			defaultValue: false));

		AddOption(Pipelining = new(
			keys: new[] { "Pipelining" },
			defaultValue: true));

		AddOption(ServerRedirectionMode = new(
			keys: new[] { "Server Redirection Mode", "ServerRedirectionMode" },
			defaultValue: SingleStoreServerRedirectionMode.Disabled));

		AddOption(ServerRsaPublicKeyFile = new(
			keys: new[] { "Server RSA Public Key File", "ServerRsaPublicKeyFile" },
			defaultValue: ""));

		AddOption(ServerSPN = new(
			keys: new[] { "Server SPN", "ServerSPN" },
			defaultValue: ""));

		AddOption(TreatTinyAsBoolean = new(
			keys: new[] { "Treat Tiny As Boolean", "TreatTinyAsBoolean" },
			defaultValue: true));

		AddOption(UseAffectedRows = new(
			keys: new[] { "Use Affected Rows", "UseAffectedRows" },
			defaultValue: false));

		AddOption(UseCompression = new(
			keys: new[] { "Use Compression", "Compress", "UseCompression" },
			defaultValue: false));

		AddOption(UseXaTransactions = new(
			keys: new[] { "Use XA Transactions", "UseXaTransactions" },
			defaultValue: true));
	}

	private const string c_tlsVersionsRegexPattern = @"\s*TLS( ?v?(1|1\.?0|1\.?1|1\.?2|1\.?3))?$";
#if NET7_0_OR_GREATER
	[GeneratedRegex(c_tlsVersionsRegexPattern, RegexOptions.IgnoreCase)]
	private static partial Regex TlsVersionsRegex();
#else
	private static Regex TlsVersionsRegex() => s_tlsVersionsRegex;
	private static readonly Regex s_tlsVersionsRegex = new(c_tlsVersionsRegexPattern, RegexOptions.IgnoreCase);
#endif
	private static readonly Dictionary<string, SingleStoreConnectionStringOption> s_options;

	private readonly IReadOnlyList<string> m_keys;
}

internal sealed class SingleStoreConnectionStringValueOption<T> : SingleStoreConnectionStringOption
	where T : struct
{
	public SingleStoreConnectionStringValueOption(IReadOnlyList<string> keys, T defaultValue, Func<T, T>? coerce = null)
		: base(keys)
	{
		m_defaultValue = defaultValue;
		m_coerce = coerce;
	}

	public T GetValue(SingleStoreConnectionStringBuilder builder) =>
		builder.TryGetValue(Key, out var objectValue) ? ChangeType(objectValue) : m_defaultValue;

	public void SetValue(SingleStoreConnectionStringBuilder builder, T value) =>
		builder.DoSetValue(Key, m_coerce is null ? value : m_coerce(value));

	public override object GetObject(SingleStoreConnectionStringBuilder builder) => GetValue(builder);

	public override void SetObject(SingleStoreConnectionStringBuilder builder, object value) => SetValue(builder, ChangeType(value));

	private T ChangeType(object objectValue)
	{
		if (typeof(T) == typeof(bool) && objectValue is string booleanString)
		{
			if (string.Equals(booleanString, "yes", StringComparison.OrdinalIgnoreCase))
				return (T) (object) true;
			if (string.Equals(booleanString, "no", StringComparison.OrdinalIgnoreCase))
				return (T) (object) false;
		}

		if ((typeof(T) == typeof(SingleStoreLoadBalance) || typeof(T) == typeof(SingleStoreSslMode) || typeof(T) == typeof(SingleStoreServerRedirectionMode) || typeof(T) == typeof(SingleStoreDateTimeKind) || typeof(T) == typeof(SingleStoreGuidFormat) || typeof(T) == typeof(SingleStoreConnectionProtocol) || typeof(T) == typeof(SingleStoreCertificateStoreLocation)) && objectValue is string enumString)
		{
			try
			{
#if NETCOREAPP2_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
				return Enum.Parse<T>(enumString, ignoreCase: true);
#else
				return (T) Enum.Parse(typeof(T), enumString, ignoreCase: true);
#endif
			}
			catch (Exception ex) when (ex is not ArgumentException)
			{
				throw new ArgumentException("Value '{0}' not supported for option '{1}'.".FormatInvariant(objectValue, typeof(T).Name), ex);
			}
		}

		try
		{
			return (T) Convert.ChangeType(objectValue, typeof(T), CultureInfo.InvariantCulture);
		}
		catch (Exception ex)
		{
			throw new ArgumentException("Invalid value '{0}' for '{1}' connection string option.".FormatInvariant(objectValue, Key), ex);
		}
	}

	private readonly T m_defaultValue;
	private readonly Func<T, T>? m_coerce;
}

internal sealed class SingleStoreConnectionStringReferenceOption<T> : SingleStoreConnectionStringOption
	where T : class
{
	public SingleStoreConnectionStringReferenceOption(IReadOnlyList<string> keys, T defaultValue, Func<T?, T>? coerce = null)
		: base(keys)
	{
		m_defaultValue = defaultValue;
		m_coerce = coerce;
	}

	public T GetValue(SingleStoreConnectionStringBuilder builder) =>
		builder.TryGetValue(Key, out var objectValue) ? ChangeType(objectValue) : m_defaultValue;

	public void SetValue(SingleStoreConnectionStringBuilder builder, T? value) =>
		builder.DoSetValue(Key, m_coerce is null ? value : m_coerce(value));

	public override object GetObject(SingleStoreConnectionStringBuilder builder) => GetValue(builder);

	public override void SetObject(SingleStoreConnectionStringBuilder builder, object value) => SetValue(builder, ChangeType(value));

	private static T ChangeType(object objectValue) =>
		(T) Convert.ChangeType(objectValue, typeof(T), CultureInfo.InvariantCulture);

	private readonly T m_defaultValue;
	private readonly Func<T?, T>? m_coerce;
}
