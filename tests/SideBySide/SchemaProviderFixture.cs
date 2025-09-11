namespace SideBySide;

public class SchemaProviderFixture : DatabaseFixture
{
	public SchemaProviderFixture()
	{
		Connection.Execute("""
		                   	DROP TABLE IF EXISTS pk_test;
		                   	CREATE ROWSTORE TABLE pk_test
		                   	(
		                   		a INT NOT NULL,
		                   		b INT NOT NULL,
		                   		c INT NOT NULL,
		                   		d INT NOT NULL,
		                   		e INT NOT NULL,
		                   		CONSTRAINT pk_test_pk PRIMARY KEY (a, b),
		                   		CONSTRAINT pk_test_uq UNIQUE INDEX (a, b, c, d, e),
		                   		INDEX pk_test_ix (c, d)
		                   	);
		                   """);
	}
}
