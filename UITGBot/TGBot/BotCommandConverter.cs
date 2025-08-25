using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using UITGBot.Core;
using UITGBot.TGBot.CommandTypes;
using Polly;

namespace UITGBot.TGBot
{
    internal class BotCommandConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(BotCommand);
        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            JObject jsonObject = JObject.Load(reader);

            if (!jsonObject.TryGetValue("CommandType", out JToken? typeToken))
            {
                throw new JsonException("Поле 'CommandType' отсутствует.");
            }

            string type = typeToken.ToString();

            BotCommand? command = type switch
            {
                "full_text" => jsonObject.ToObject<TextCommand>(serializer),
                "file" => jsonObject.ToObject<FileCommand>(serializer),
                "image" => jsonObject.ToObject<ImageCommand>(serializer),
                "script" => jsonObject.ToObject<ScriptCommand>(serializer),
                "random_text" => jsonObject.ToObject<RandomTextCommand>(serializer),
                "random_file" => jsonObject.ToObject<RandomFileCommand>(serializer),
                "random_image" => jsonObject.ToObject<RandomImageCommand>(serializer),
                "random_script" => jsonObject.ToObject<RandomScriptCommand>(serializer),
                "remote_file" => jsonObject.ToObject<RemoteFileCommand>(serializer),
                "simple" => jsonObject.ToObject<SimpleCommand>(serializer),
                _ => throw new JsonException($"Неизвестный тип команды: {type}")
            };
            if (command != null)
            {
                var exists = Storage.BotCommands.Any(c => string.Equals(c.Name?.Trim(), command.Name?.Trim(), StringComparison.OrdinalIgnoreCase));
                // Тут тоже нужно добавить логирование, но ИМХО пока похуям
                if (!exists)
                {
                    Storage.Statisticks.botActionsCount++;
                    if (command.Enabled) Storage.Statisticks.botActiveActionsCount++;
                }
            }
            switch (command?.CommandType.ToLower().Trim())
            {
                case "full_text":
                    Storage.Statisticks.ActionsCountTypeOf_full_text++;
                    break;
                case "file":
                    Storage.Statisticks.ActionsCountTypeOf_file++;
                    break;
                case "image":
                    Storage.Statisticks.ActionsCountTypeOf_image++;
                    break;
                case "script":
                    Storage.Statisticks.ActionsCountTypeOf_script++;
                    break;
                case "random_text":
                    Storage.Statisticks.ActionsCountTypeOf_random_text++;
                    break;
                case "random_file":
                    Storage.Statisticks.ActionsCountTypeOf_random_file++;
                    break;
                case "random_image":
                    Storage.Statisticks.ActionsCountTypeOf_random_image++;
                    break;
                case "random_script":
                    Storage.Statisticks.ActionsCountTypeOf_random_script++;
                    break;
                case "remote_file":
                    Storage.Statisticks.ActionsCountTypeOf_remote_file++;
                    break;
                case "simple":
                    Storage.Statisticks.ActionsCountTypeOf_simple++;
                    break;
            }
            return command!;
        }
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }

        /*
            public override BotCommand? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                using JsonDocument doc = JsonDocument.ParseValue(ref reader);
                var root = doc.RootElement;

                if (!root.TryGetProperty("CommandType", out var typeProperty))
                {
                    throw new JsonException("Поле 'CommandType' отсутствует.");
                }

                string type = typeProperty.GetString() ?? string.Empty;

                BotCommand? command = type switch
                {
                    "text" => JsonSerializer.Deserialize<TextCommand>(root.GetRawText(), options),
                    "file" => JsonSerializer.Deserialize<FileCommand>(root.GetRawText(), options),
                    "image" => JsonSerializer.Deserialize<ImageCommand>(root.GetRawText(), options),
                    "script" => JsonSerializer.Deserialize<ScriptCommand>(root.GetRawText(), options),
                    "random_text" => JsonSerializer.Deserialize<RandomTextCommand>(root.GetRawText(), options),
                    "random_file" => JsonSerializer.Deserialize<RandomFileCommand>(root.GetRawText(), options),
                    "random_image" => JsonSerializer.Deserialize<RandomImageCommand>(root.GetRawText(), options),
                    "random_script" => JsonSerializer.Deserialize<RandomScriptCommand>(root.GetRawText(), options),
                    _ => null
                };
                if (command == null) Storage.Logger?.Logger?.Warning($"Неизвестный тип команды: {type}. Команда будет пропущена.");
                else
                {
                    if (command.Verify())
                    {
                        Storage.Logger?.Logger?.Information($"Успешно верифицирована команда: {command.Name}");
                    }
                    else
                    {
                        Storage.Logger?.Logger?.Warning($"Ошибка верификации команды: {command.Name}. Команда будет пропущена.");
                        command = null;
                    }
                }
                return command;
            }

            public override void Write(Utf8JsonWriter writer, BotCommand value, JsonSerializerOptions options)
            {
                JsonSerializer.Serialize(writer, (object)value, value.GetType(), options);
            }
         */
    }
}
