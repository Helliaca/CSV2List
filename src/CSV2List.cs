using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CSV2List
{
    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = false)]
    public class CSVField : System.Attribute
    {
        public string FieldName;

        public CSVField(string FieldName) {
            this.FieldName = FieldName;
        }
    }


    public static class CSVReader
    {
        public static List<T> ReadFromCSV<T>(string filepath, char delimeter = ';') where T : new()
        {
            // Create return list
            var result = new List<T>();

            // Read file contents
            var lines = File.ReadAllLines(filepath);

            if (lines.Length < 2)
            {
                throw new Exception("Tried to read a CSV with no content!");
            }

            // Get headers
            var headers = lines[0].Split(delimeter);

            // Read data
            for (int i = 1; i < lines.Length; i++)
            {
                // Create new object of type T
                var obj = new T();

                // Get values of the current CSV entry
                var values = lines[i].Split(delimeter);

                // Get all the properties
                foreach (var prop in typeof(T).GetFields())
                {
                    // If they have a CSVField property
                    var attribute = prop.GetCustomAttribute<CSVField>();
                    if (attribute != null)
                    {
                        var index = Array.IndexOf(headers, attribute.FieldName);
                        if (index != -1)
                        {
                            var value = Convert.ChangeType(values[index], prop.FieldType);
                            prop.SetValue(obj, value);
                        }
                        else
                        {
                            Console.WriteLine($"Tried to set object from CSV. Object has a field {attribute.FieldName}, but the CSV file does not contain it! Ignoring...");
                        }
                    }
                }

                result.Add(obj);
            }

            return result;
        }
    }

    public static class CSVWriter { 

        public static void WriteToCSV<T>(List<T> list, string filepath, char delimeter=';') where T : new()
        {
            var sb = new StringBuilder();

            // Get properties with CSVField attribute
            var properties = typeof(T).GetFields()
                .Where(prop => Attribute.IsDefined(prop, typeof(CSVField)));

            // Write headers
            sb.AppendLine(string.Join(delimeter, properties.Select(prop => ((CSVField)Attribute.GetCustomAttribute(prop, typeof(CSVField))).FieldName)));

            // Write data
            foreach (var obj in list)
            {
                sb.AppendLine(string.Join(delimeter, properties.Select(prop => prop.GetValue(obj))));
            }

            // Ensure directory exists
            var directory = Path.GetDirectoryName(filepath);
            Directory.CreateDirectory(directory);

            File.WriteAllText(filepath, sb.ToString());
        }
    }
}
