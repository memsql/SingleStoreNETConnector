# SingleStoreGuidFormat enumeration

Determines which column type (if any) should be read as a `System.Guid`.

```csharp
public enum SingleStoreGuidFormat
```

## Values

| name | value | description |
| --- | --- | --- |
| Default | `0` | Same as `Char36` if `OldGuids=False`; same as `LittleEndianBinary16` if `OldGuids=True`. |
| None | `1` | No column types are read/written as a |
| Char36 | `2` | All `CHAR(36)` columns are read/written as a `Guid` using lowercase hex with hyphens, which matches [UUID()](https://dev.mysql.com/doc/refman/8.0/en/miscellaneous-functions.html#function_uuid). |
| Char32 | `3` | All `CHAR(32)` columns are read/written as a `Guid` using lowercase hex without hyphens. |
| Binary16 | `4` | All `BINARY(16)` columns are read/written as a `Guid` using big-endian byte order, which matches [UUID_TO_BIN(x)](https://dev.mysql.com/doc/refman/8.0/en/miscellaneous-functions.html#function_uuid-to-bin). |
| TimeSwapBinary16 | `5` | All `BINARY(16)` columns are read/written as a `Guid` using big-endian byte order with time parts swapped, which matches [UUID_TO_BIN(x,1)](https://dev.mysql.com/doc/refman/8.0/en/miscellaneous-functions.html#function_uuid-to-bin). |
| LittleEndianBinary16 | `6` | All `BINARY(16)` columns are read/written as a `Guid` using little-endian byte order, i.e. the byte order used by ToByteArray and Byte[]). |

## See Also

* namespace [SingleStoreConnector](../SingleStoreConnector.md)

<!-- DO NOT EDIT: generated by xmldocmd for SingleStoreConnector.dll -->
