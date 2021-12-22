using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Json;
using Serilog.Parsing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Serilog.Sinks.Datadog.Logs.Sinks.Datadog
{
    /// <summary>
    /// Formats log events in a simple JSON structure in accordance with the expected structure expected by the Datadog API. Instances of this class
    /// are safe for concurrent access by multiple threads.
    /// </summary>
    internal class DatadogFormatter : ITextFormatter
    {
        private static readonly HashSet<string> SpecialProperties = new HashSet<string>() { "ddsource", "ddservice", "ddhost", "ddtags" };

        private static readonly IDictionary<Type, Action<object, bool, TextWriter>> _literalWriters = new Dictionary<Type, Action<object, bool, TextWriter>>
            {
                { typeof(bool), (v, _, w) => WriteBoolean((bool)v, w) },
                { typeof(char), (v, _, w) => WriteString(((char)v).ToString(), w) },
                { typeof(byte), WriteToString },
                { typeof(sbyte), WriteToString },
                { typeof(short), WriteToString },
                { typeof(ushort), WriteToString },
                { typeof(int), WriteToString },
                { typeof(uint), WriteToString },
                { typeof(long), WriteToString },
                { typeof(ulong), WriteToString },
                { typeof(float), (v, _, w) => WriteSingle((float)v, w) },
                { typeof(double), (v, _, w) => WriteDouble((double)v, w) },
                { typeof(decimal), WriteToString },
                { typeof(string), (v, _, w) => WriteString((string)v, w) },
                { typeof(DateTime), (v, _, w) => WriteDateTime((DateTime)v, w) },
                { typeof(DateTimeOffset), (v, _, w) => WriteOffset((DateTimeOffset)v, w) },
                { typeof(ScalarValue), (v, q, w) => WriteLiteral(((ScalarValue)v).Value, w, q) },
                { typeof(SequenceValue), (v, _, w) => WriteSequence(((SequenceValue)v).Elements, w) },
                { typeof(DictionaryValue), (v, _, w) => WriteDictionary(((DictionaryValue)v).Elements, w) },
                { typeof(StructureValue), (v, _, w) => WriteStructure(((StructureValue)v).TypeTag, ((StructureValue)v).Properties, w) },
            };
        private readonly bool _renderMessage;

        public DatadogFormatter(bool renderMessage)
        {
            _renderMessage = renderMessage;

        }

        /// <summary>
        /// Format the log event into the output.
        /// </summary>
        /// <param name="logEvent">The event to format.</param>
        /// <param name="output">The output.</param>
        /// <exception cref="ArgumentNullException">When <paramref name="logEvent"/> is <code>null</code></exception>
        /// <exception cref="ArgumentNullException">When <paramref name="output"/> is <code>null</code></exception>
        public void Format(LogEvent logEvent, TextWriter output)
        {
            if (logEvent == null) throw new ArgumentNullException(nameof(logEvent));
            if (output == null) throw new ArgumentNullException(nameof(output));

            output.Write("{");

            var delim = "";
            WriteJsonProperty("Timestamp", logEvent.Timestamp, ref delim, output);
            WriteJsonProperty("level", logEvent.Level, ref delim, output);

            WriteJsonProperty("MessageTemplate", logEvent.MessageTemplate, ref delim, output);
            if (_renderMessage)
            {
                var message = logEvent.RenderMessage();
                WriteJsonProperty("message", message, ref delim, output);
            }

            if (logEvent.Exception != null)
            {
                WriteJsonProperty("Exception", logEvent.Exception, ref delim, output);
            }


            if (logEvent.Properties.Count != 0)
            {
                foreach (var property in logEvent.Properties.Where(p => SpecialProperties.Contains(p.Key)))
                {
                    switch (property.Key)
                    {
                        case "ddservice":
                            WriteJsonProperty("service", property.Value, ref delim, output);
                            break;
                        case "ddhost":
                            WriteJsonProperty("host", property.Value, ref delim, output);
                            break;
                        default:
                            WriteJsonProperty(property.Key, property.Value, ref delim, output);
                            break;
                    }
                }
                WriteProperties(logEvent.Properties.Where(p => !SpecialProperties.Contains(p.Key)), output);
            }

            var tokensWithFormat = logEvent.MessageTemplate.Tokens
                .OfType<PropertyToken>()
                .Where(pt => pt.Format != null)
                .GroupBy(pt => pt.PropertyName)
                .ToArray();

            if (tokensWithFormat.Length != 0)
            {
                WriteRenderings(tokensWithFormat, logEvent.Properties, output);
            }

            output.Write("}");
            output.Write(Environment.NewLine);

        }

        static void WriteProperties(IEnumerable<KeyValuePair<string, LogEventPropertyValue>> properties, TextWriter output)
        {
            output.Write(",\"{0}\":{{", "Properties");
            WritePropertiesValues(properties, output);
            output.Write("}");
        }

        static void WriteRenderings(IGrouping<string, PropertyToken>[] tokensWithFormat, IReadOnlyDictionary<string, LogEventPropertyValue> properties, TextWriter output)
        {
            output.Write(",\"{0}\":{{", "Renderings");
            WriteRenderingsValues(tokensWithFormat, properties, output);
            output.Write("}");
        }

        static void WriteRenderingsValues(IGrouping<string, PropertyToken>[] tokensWithFormat, IReadOnlyDictionary<string, LogEventPropertyValue> properties, TextWriter output)
        {
            var rdelim = "";
            foreach (var ptoken in tokensWithFormat)
            {
                output.Write(rdelim);
                rdelim = ",";
                output.Write("\"");
                output.Write(ptoken.Key);
                output.Write("\":[");

                var fdelim = "";
                foreach (var format in ptoken)
                {
                    output.Write(fdelim);
                    fdelim = ",";

                    output.Write("{");
                    var eldelim = "";

                    WriteJsonProperty("Format", format.Format, ref eldelim, output);

                    var sw = new StringWriter();
                    format.Render(properties, sw);
                    WriteJsonProperty("Rendering", sw.ToString(), ref eldelim, output);

                    output.Write("}");
                }

                output.Write("]");
            }
        }


        static void WritePropertiesValues(IEnumerable<KeyValuePair<string, LogEventPropertyValue>> properties, TextWriter output)
        {
            var precedingDelimiter = "";
            foreach (var property in properties)
            {
                WriteJsonProperty(property.Key, property.Value, ref precedingDelimiter, output);
            }
        }


        static void WriteLiteral(object value, TextWriter output, bool forceQuotation = false)
        {
            if (value == null)
            {
                output.Write("null");
                return;
            }

            if (_literalWriters.TryGetValue(value.GetType(), out var writer))
            {
                writer(value, forceQuotation, output);
                return;
            }

            WriteString(value.ToString() ?? "", output);
        }

        static void WriteStructure(string typeTag, IEnumerable<LogEventProperty> properties, TextWriter output)
        {
            output.Write("{");

            var delim = "";
            if (typeTag != null)
                WriteJsonProperty("_typeTag", typeTag, ref delim, output);

            foreach (var property in properties)
                WriteJsonProperty(property.Name, property.Value, ref delim, output);

            output.Write("}");
        }

        static void WriteJsonProperty(string name, object value, ref string precedingDelimiter, TextWriter output)
        {
            output.Write(precedingDelimiter);
            output.Write("\"");
            output.Write(name);
            output.Write("\":");
            WriteLiteral(value, output);
            precedingDelimiter = ",";
        }

        static void WriteSequence(IEnumerable elements, TextWriter output)
        {
            output.Write("[");
            var delim = "";
            foreach (var value in elements)
            {
                output.Write(delim);
                delim = ",";
                WriteLiteral(value, output);
            }
            output.Write("]");
        }

        static void WriteDictionary(IReadOnlyDictionary<ScalarValue, LogEventPropertyValue> elements, TextWriter output)
        {
            output.Write("{");
            var delim = "";
            foreach (var element in elements)
            {
                output.Write(delim);
                delim = ",";
                WriteLiteral(element.Key, output, forceQuotation: true);
                output.Write(":");
                WriteLiteral(element.Value, output);
            }
            output.Write("}");
        }

        static void WriteToString(object number, bool quote, TextWriter output)
        {
            if (quote) output.Write('"');

            if (number is IFormattable fmt)
                output.Write(fmt.ToString(null, CultureInfo.InvariantCulture));
            else
                output.Write(number.ToString());

            if (quote) output.Write('"');
        }

        static void WriteBoolean(bool value, TextWriter output)
        {
            output.Write(value ? "true" : "false");
        }

        static void WriteSingle(float value, TextWriter output)
        {
            output.Write(value.ToString("R", CultureInfo.InvariantCulture));
        }

        static void WriteDouble(double value, TextWriter output)
        {
            output.Write(value.ToString("R", CultureInfo.InvariantCulture));
        }

        static void WriteOffset(DateTimeOffset value, TextWriter output)
        {
            output.Write("\"");
            output.Write(value.ToString("o"));
            output.Write("\"");
        }

        static void WriteDateTime(DateTime value, TextWriter output)
        {
            output.Write("\"");
            output.Write(value.ToString("o"));
            output.Write("\"");
        }



        static void WriteString(string value, TextWriter output, bool quote = true)
        {
            if (quote)
            {
                JsonValueFormatter.WriteQuotedJsonString(value, output);
            }
            else
            {
                output.Write(value);
            }
        }

    }
}
