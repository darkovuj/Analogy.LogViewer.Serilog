﻿using Analogy.Interfaces;
using Analogy.LogViewer.Serilog.DataTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using Serilog.Formatting.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Analogy.LogViewer.Serilog
{
    public class JsonFormatterParser
    {
        private  IMessageFields messageFields;

        public JsonFormatterParser(IMessageFields messageFields)
        {
            this.messageFields = messageFields;
        }
        public async Task<IEnumerable<AnalogyLogMessage>> Process(string fileName, CancellationToken token,
            ILogMessageCreatedHandler messagesHandler)
        {
            //var formatter = new JsonFormatter();
            List<AnalogyLogMessage> parsedMessages = new List<AnalogyLogMessage>();
            try
            {
                using (var analogy = new LoggerConfiguration().WriteTo.Analogy()
                    .CreateLogger())
                {
                    using (var fileStream =
                        new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {

                        if (fileName.EndsWith(".gz", StringComparison.InvariantCultureIgnoreCase))
                        {
                            using (var gzStream = new GZipStream(fileStream, CompressionMode.Decompress))
                            {
                                using (var streamReader = new StreamReader(gzStream, encoding: Encoding.UTF8))
                                {
                                    string json;
                                    while ((json = await streamReader.ReadLineAsync()) != null)
                                    {
                                        var data = JsonConvert.DeserializeObject(json);
                                        var evt = LogEventReader.ReadFromJObject(data as JObject, messageFields);
                                        {
                                            analogy.Write(evt);
                                            AnalogyLogMessage m = CommonParser.ParseLogEventProperties(evt);
                                            parsedMessages.Add(m);
                                        }
                                    }

                                }
                            }
                        }


                        using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
                        {
                            string json;
                            while ((json = await streamReader.ReadLineAsync()) != null)
                            {
                                var data = JsonConvert.DeserializeObject(json);
                                var evt = LogEventReader.ReadFromJObject(data as JObject, messageFields);
                                {
                                    analogy.Write(evt);
                                    AnalogyLogMessage m = CommonParser.ParseLogEventProperties(evt);
                                    parsedMessages.Add(m);
                                }
                            }
                        }

                    }

                    messagesHandler.AppendMessages(parsedMessages, fileName);
                    return parsedMessages;
                }
            }
            catch (Exception e)
            {
                AnalogyLogMessage empty = new AnalogyLogMessage($"Error reading file {fileName}: Error: {e.Message}",
                    AnalogyLogLevel.Error, AnalogyLogClass.General, "Analogy", "None");
                empty.Source = nameof(CompactJsonFormatParser);
                empty.Module = "Analogy.LogViewer.Serilog";
                parsedMessages.Add(empty);
                messagesHandler.AppendMessages(parsedMessages, fileName);
                return parsedMessages;
            }
        }
    }
}