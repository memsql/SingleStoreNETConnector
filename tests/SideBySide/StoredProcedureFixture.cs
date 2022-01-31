namespace SideBySide;

public class StoredProcedureFixture : DatabaseFixture
{
	public StoredProcedureFixture()
	{
		Connection.Open();
		Connection.Execute(@"CREATE OR REPLACE FUNCTION echof(
				name VARCHAR(63)
			) RETURNS VARCHAR(63) AS
			BEGIN
				RETURN name;
			END;
		");

		Connection.Execute(@"CREATE OR REPLACE FUNCTION failing_function()
			RETURNS DECIMAL(10,5) AS
			DECLARE v1 DECIMAL(10,5);
			BEGIN
				v1 = 1/0;
				RETURN v1;
			END;
		");

		Connection.Execute(@"CREATE OR REPLACE PROCEDURE echop(
				name VARCHAR(63)
			) AS
			BEGIN
				ECHO SELECT name;
			END;
		");

		Connection.Execute(@"CREATE OR REPLACE PROCEDURE circle(
				radius DOUBLE,
				height DOUBLE,
				name VARCHAR(63)
			) AS
			DECLARE
				diameter DOUBLE;
				circumference DOUBLE;
				area DOUBLE;
				volume DOUBLE;
				shape VARCHAR(63);
			BEGIN
				diameter = radius * 2;
				circumference = diameter * PI();
				area = PI() * POW(radius, 2);
				volume = area * height;
				shape = 'circle';
				ECHO SELECT CONCAT(name, shape), diameter, circumference, area, volume, shape;
			END;
		");

		Connection.Execute(@"CREATE OR REPLACE PROCEDURE out_string() AS
			DECLARE value VARCHAR(100);
			BEGIN
				value = 'test value';
			END;
		");

		Connection.Execute(@"CREATE OR REPLACE PROCEDURE echo_null() AS
			DECLARE
				string_value VARCHAR(100);
				int_value INT;
			BEGIN
				string_value = NULL;
				int_value = NULL;
				ECHO SELECT string_value, int_value;
			END;
		");

		Connection.Execute(@"drop table if exists sproc_multiple_rows;
			create table sproc_multiple_rows (
				value integer not null primary key auto_increment,
				name text not null
			);
			insert into sproc_multiple_rows values
			(1, 'one'),
			(2, 'two'),
			(3, 'three'),
			(4, 'four'),
			(5, 'five'),
			(6, 'six'),
			(7, 'seven'),
			(8, 'eight');

			create or replace procedure number_multiples (factor int) as
			begin
				echo select name from sproc_multiple_rows
				where mod(value, factor) = 0
				order by name;
			end;
		");

		Connection.Execute(@"create or replace procedure multiple_result_sets (pivot int) as
			begin
				echo select name from sproc_multiple_rows where value < pivot order by name;
				echo select name from sproc_multiple_rows where value > pivot order by name;
			end;
		");

		Connection.Execute(@"create or replace procedure number_lister (high int) returns int as
			declare
			  i int = 1;
			begin
			  WHILE (i <= high) LOOP
				echo select value, name from sproc_multiple_rows
				where value <= high
				order by value;
				i = i + 1;
			  END LOOP;
			  RETURN high + 1;
			end;
		");

		Connection.Execute(@"create or replace procedure `dotted.name`() as
			begin
				echo select 1, 2, 3;
			end;
		");

		Connection.Execute(@"CREATE OR REPLACE PROCEDURE `GetTime`() AS
			BEGIN
				ECHO SELECT CURTIME();
			END;
		");

		Connection.Execute(@"CREATE OR REPLACE PROCEDURE EnumProcedure(input enum ('One', 'Two', 'Three')) as
			BEGIN
				ECHO SELECT input;
			END;
		");

		if (AppConfig.SupportsJson)
		{
			Connection.Execute(@"CREATE OR REPLACE PROCEDURE `SetJson`(vJson JSON) as
				BEGIN
					ECHO SELECT vJson;
				END
			");
		}
	}
}
