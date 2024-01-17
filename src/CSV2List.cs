using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using static System.Net.Mime.MediaTypeNames;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Drawing;

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
        // Splits a string by a delimeter, respecting double-quotes
        private static string[] CSVLineSplit(ReadOnlySpan<char> line, char delimeter)
        {
            List<string> result = new List<string>();

            int start = 0;
            for (int i=0; i<line.Length; i++)
            {
                // quotation mark -> skip ahead until we find a matching quoation mark
                if (line[i] == '\"')
                {
                    i++;
                    while (i < line.Length && line[i] != '\"') i++;

                    // Special case: If a string literal is completely isolated by delimeters/newlines:
                    // remove the quotation marks from it and take only the value inside
                    if (line[start] == '\"' && (i == line.Length-1 || line[i+1] == delimeter))
                    {
                        string newstring = line.Slice(start+1, i - start-1).ToString();
                        result.Add(newstring);
                        start = i + 1;
                    }
                }
                // regular entry
                else if (line[i] == delimeter)
                {
                    string newstring = line.Slice(start, i - start).ToString();
                    result.Add(newstring);
                    start = i + 1;
                }
            }

            // Last entry
            if(start < line.Length)
            {
                string newstring = line.Slice(start, line.Length - start).ToString();
                result.Add(newstring);
            }

            return result.ToArray();
        }

        // tries to parse/convert a string to a given type
        public static object TryConvert(string input, Type targetType)
        {
            try
            {
                // Check if the targetType is nullable and get the underlying type
                Type conversionType = Nullable.GetUnderlyingType(targetType) ?? targetType;

                // Convert the input to the targetType
                return Convert.ChangeType(input, conversionType, CultureInfo.InvariantCulture);
            }
            catch
            {
                // If conversion fails, return null or the default value for value types
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
            }
        }

        public static List<T> ReadFromCSV<T>(string filepath, char delimeter = ';') where T : new()
        {
            // Create return list
            var result = new List<T>();

            // Read file contents
            var lines = File.ReadAllLines(filepath);

            if (lines.Length < 1)
            {
                throw new Exception("Tried to read a CSV with no content!");
            }

            // Get headers
            //var headers = lines[0].Split(delimeter);
            //var headers = CSVSplitter.Split(lines[0]);
            var headers = CSVLineSplit(lines[0], delimeter);

            // List of all CSVField fields in type T
            List<FieldInfo> fields = new List<FieldInfo>();

            var fs = typeof(T).GetFields();

            foreach(var header in headers)
            {
                var f = fs.FirstOrDefault(f =>
                {
                    var att = f.GetCustomAttribute<CSVField>();
                    return att != null && att.FieldName == header;
                });
                fields.Add(f);
            }

            // Read data
            for (int i = 1; i < lines.Length; i++)
            {
                // Create new object of type T
                var obj = new T();
                

                // Get values of the current CSV entry
                //var values = lines[i].Split(delimeter);
                //var values = CSVSplitter.Split(lines[i]);
                var values = CSVLineSplit(lines[i], delimeter);


                for(int j = 0; j < fields.Count; j++)
                {
                    if (fields[j] != null && j<values.Length)
                    {
                        var value = TryConvert(values[j], fields[j].FieldType);
                        fields[j].SetValue(obj, value);
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
                sb.AppendLine(string.Join(delimeter, properties.Select(prop => {
                    string r = prop.GetValue(obj).ToString();
                    if(r.Contains(delimeter)) r = "\""+r+"\"";
                    return r;
                })));
            }

            // Ensure directory exists
            var directory = Path.GetDirectoryName(filepath);
            Directory.CreateDirectory(directory);

            File.WriteAllText(filepath, sb.ToString());
        }
    }
}
