# Tests

## Side-by-side Tests

The `SideBySide` project is intended to verify that SingleStoreSqlConnector doesn't break compatibility
with Connector/NET;

The tests require a SingleStore server. The simplest way to run one is with [Docker](https://github.com/singlestore-labs/singlestoredb-dev-image):

    docker run \
    -d --name singlestoredb-dev \
    -e ROOT_PASSWORD="pass" \
    -p 3306:3306 -p 8080:8080 -p 9000:9000 \
    ghcr.io/singlestore-labs/singlestoredb-dev:latest


Copy the file `SideBySide/config.json.example` to `SideBySide/config.json`, then edit
the `config.json` file in order to connect to your server. Set the following options appropriately:

* `Data.ConnectionString`: The full connection string to your server. You should specify a database name. If the database does not exist, the test will attempt to create it.
* `Data.PasswordlessUser`: (Optional) A user account in your database with no password and no roles.
* `Data.SecondaryDatabase`: (Optional) A second database on your server that the test user has permission to access.
* `Data.CertificatesPath`: (Optional) The absolute path to the server and client certificates folder (i.e., the `.ci/server/certs` folder in this repo).
* `Data.MySqlBulkLoaderLocalCsvFile`: (Optional) The path to a test CSV file.
* `Data.MySqlBulkLoaderLocalTsvFile`: (Optional) The path to a test TSV file.
* `Data.UnsupportedFeatures`: A comma-delimited list of `ServerFeature` enum values that your test database server does *not* support
  * `CachingSha2Password`: a user named `caching-sha2-user` exists on your server and uses the `caching_sha2_password` auth plugin
  * `Ed25519`: a user named `ed25519user` exists on your server and uses the `client_ed25519` auth plugin
  * `ErrorCodes`: server returns error codes in error packet (some MySQL proxies do not)
  * `Json`: the `JSON` data type (MySQL 5.7 and later)
  * `LargePackets`: large packets (over 4MB)
  * `RoundDateTime`: server rounds `datetime` values to the specified precision (not implemented in MariaDB)
  * `RsaEncryption`: server supports RSA public key encryption (for `sha256_password` and `caching_sha2_password`)
  * `SessionTrack`: server supports `CLIENT_SESSION_TRACK` capability (MySQL 5.7 and later)
  * `Sha256Password`: a user named `sha256user` exists on your server and uses the `sha256_password` auth plugin
  * `StoredProcedures`: create and execute stored procedures
  * `Timeout`: server can cancel queries promptly (so timed tests don't time out)
  * `Tls11`: server supports TLS 1.1
  * `Tls12`: server supports TLS 1.2
  * `Tls13`: server supports TLS 1.3
  * `UnixDomainSocket`: server is accessible via a Unix domain socket
  * `UuidToBin`: server supports `UUID_TO_BIN` (MySQL 8.0 and later)
  * `CancelSleepSuccessfully`: a "SLEEP" command produces a result set when it is cancelled, not an error payload.

## Running Tests

To run the tests against SingleStoreConnector:

```
cd tests\SideBySide
dotnet test -c Release -f net8.0
```
