# CSV2List
Simple, lightweight C# library to read and write Lists of objects to CSV files.

(C# 2.1+)

## Usage

C# Datacontainer class:

```chsarp
public class MyDataContainer
{
    [CSV2List.CSVField("id")]
    public int id;

    [CSV2List.CSVField("name")]
    public string name;
}
```

CSV file:

```
id;name
1;robert
52;sarah
32;philip
```

Reading/Writing:

```csharp
List<MyDataContainer> myList = CSVReader.ReadFromCSV<MyDataContainer>("my_file.csv");

myList[1].id = 42;

CSVWriter.WriteToCSV(myList, "output/my_corrected_file.csv");
```