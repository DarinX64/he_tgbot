﻿using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace Telegram.Bot.Examples.Echo
{
    public static class Program
    {
        private static TelegramBotClient Bot;
        private static String dbFileName;
        private static SQLiteConnection m_dbConn;
        private static SQLiteCommand m_sqlCmd;

        private const string cmdGetInfoForCountry = "Получить инфу по названию страны";


        public static async Task Main()
        {
            Bot = new TelegramBotClient(Configuration.SecondBotToken);
            var me = await Bot.GetMeAsync();
            Console.Title = me.Username;

            //Открываем конехт к бд
            m_dbConn = new SQLiteConnection();
            m_sqlCmd = new SQLiteCommand();
            dbFileName = "humandb.db";
            m_dbConn = new SQLiteConnection("Data Source=" + dbFileName + ";Version=3;");
            m_dbConn.Open();

            Bot.OnMessage += BotOnMessageReceived;
            Bot.OnMessageEdited += BotOnMessageReceived;
            Bot.OnCallbackQuery += BotOnCallbackQueryReceived;
            Bot.OnInlineQuery += BotOnInlineQueryReceived;
            Bot.OnInlineResultChosen += BotOnChosenInlineResultReceived;
            Bot.OnReceiveError += BotOnReceiveError;

            Bot.StartReceiving(Array.Empty<UpdateType>());
            Console.WriteLine($"Start listening for @{me.Username}");

            Console.ReadLine();
            m_dbConn.Close();
            Bot.StopReceiving();
        }

        private static async void BotOnMessageReceived(object sender, MessageEventArgs messageEventArgs)
        {
            var message = messageEventArgs.Message;
            if (message == null || message.Type != MessageType.Text)
                return;
            

            switch (message.Text.Split(' ').First())
            {
                case cmdGetInfoForCountry:
                    await getInforForCountry(message);
                    break;
                case "/dbcall": //пример /dbcall select * from humans
                    await dbCall(message);
                    break;
                case "/dbinsert": //пример /dbinsert -F I O -CITY -DD.MM.YYYY -ANY JOB
                    await dbInsert(message);
                    break;

                default:
                    await Usage(message);
                    break;
            }

            await SendReplyKeyboard(message);

            async Task getInforForCountry(Message msg) // ВОТ СЮДЫ ЛОГИКУ РАБОТЫ С ПОЛУЧЕННЫМИ ДАННЫМИ ДЛЯ СТРАНЫЫЫ
            {


                await Bot.SendTextMessageAsync(
                   chatId: message.Chat.Id,
                   text: "Choose",
                   replyMarkup: new ReplyKeyboardRemove()

               );
            }
            async Task SendReplyKeyboard(Message msg)
            {
                var replyKeyboardMarkup = new ReplyKeyboardMarkup(
                    new KeyboardButton[][]
                    {
                        new KeyboardButton[] { cmdGetInfoForCountry, "1.2" },
                        new KeyboardButton[] { "2.1", "2.2" },
                    },
                    resizeKeyboard: true
                );

                await Bot.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Choose",
                    replyMarkup: replyKeyboardMarkup

                );
            }
            async Task SendInlineKeyboard(Message msg)
            {
                await Bot.SendChatActionAsync(message.Chat.Id, ChatAction.Typing);

                // Simulate longer running task
                await Task.Delay(500);

                var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    // first row
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("1.1", "11"),
                        InlineKeyboardButton.WithCallbackData("1.2", "12"),
                    },
                    // second row
                    new []
                    {
                        InlineKeyboardButton.WithCallbackData("2.1", "21"),
                        InlineKeyboardButton.WithCallbackData("2.2", "22"),
                    }
                });
                await Bot.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Choose",
                    replyMarkup: inlineKeyboard
                );
            }
            async Task dbInsert(Message msg)
            {
                if (!System.IO.File.Exists(dbFileName))
                    SQLiteConnection.CreateFile(dbFileName);

                try
                {
                    string args = msg.Text.Remove(0, 9);

                    string fio = args.Split('-')[1];
                    string city = args.Split('-')[2];
                    string date = args.Split('-')[3];
                    string job = args.Split('-')[4];

                    m_sqlCmd.Connection = m_dbConn;
                    m_sqlCmd.CommandText = String.Format(@"INSERT INTO Humans(fio,city,date,job) values ('{0}','{1}','{2}','{3}')",fio,city,date,job);
                    m_sqlCmd.ExecuteNonQuery();

                }
                catch (SQLiteException ex)
                {
                    Console.WriteLine(ex.Message);
                }

                await Bot.SendTextMessageAsync(
                    chatId: msg.Chat.Id,
                    text: "Inserted",
                    replyMarkup: new ReplyKeyboardRemove()
                );
            }
            async Task dbCall(Message msg)
            {
                String sqlQuery = msg.Text.Remove(0,7); //удаляю /dbcall 
                DataTable dTable = new DataTable();

                if (!System.IO.File.Exists(dbFileName))
                    SQLiteConnection.CreateFile(dbFileName);

                try
                { 
                    SQLiteDataAdapter adapter = new SQLiteDataAdapter(sqlQuery, m_dbConn);//магия
                    adapter.Fill(dTable);//магия
                    Console.WriteLine(dTable.Rows[0][1]); //покажу первый столбец нулевой строки полученого ответа от дб

                }
                catch (SQLiteException ex)
                {
                    Console.WriteLine(ex.Message);
                }

                await Bot.SendTextMessageAsync(
                    chatId: msg.Chat.Id,
                    text: dTable.Rows[0][1].ToString(),
                    replyMarkup: new ReplyKeyboardRemove()
                ) ;
            }
            async Task Usage(Message message1)
            {
                
                const string usage = "Usage:\n";
                await Bot.SendTextMessageAsync(
                    chatId: message1.Chat.Id,
                    text: usage,
                    replyMarkup: new ReplyKeyboardRemove()
                );
            }
        }

        // Process Inline Keyboard callback data
        private static async void BotOnCallbackQueryReceived(object sender, CallbackQueryEventArgs callbackQueryEventArgs)
        {
            var callbackQuery = callbackQueryEventArgs.CallbackQuery;

            await Bot.AnswerCallbackQueryAsync(
                callbackQueryId: callbackQuery.Id,
                text: $"Received {callbackQuery.Data}"
            );

            await Bot.SendTextMessageAsync(
                chatId: callbackQuery.Message.Chat.Id,
                text: $"Received {callbackQuery.Data}"
            );
        }
        #region Inline Mode
        private static async void BotOnInlineQueryReceived(object sender, InlineQueryEventArgs inlineQueryEventArgs)
        {
            Console.WriteLine($"Received inline query from: {inlineQueryEventArgs.InlineQuery.From.Id}");

            InlineQueryResultBase[] results = {
                // displayed result
                new InlineQueryResultArticle(
                    id: "3",
                    title: "TgBots",
                    inputMessageContent: new InputTextMessageContent(
                        "hello"
                    )
                )
            };
            await Bot.AnswerInlineQueryAsync(
                inlineQueryId: inlineQueryEventArgs.InlineQuery.Id,
                results: results,
                isPersonal: true,
                cacheTime: 0
            );
        }
        private static void BotOnChosenInlineResultReceived(object sender, ChosenInlineResultEventArgs chosenInlineResultEventArgs)
        {
            Console.WriteLine($"Received inline result: {chosenInlineResultEventArgs.ChosenInlineResult.ResultId}");
        }
        #endregion
        private static void BotOnReceiveError(object sender, ReceiveErrorEventArgs receiveErrorEventArgs)
        {
            Console.WriteLine("Received error: {0} — {1}",
                receiveErrorEventArgs.ApiRequestException.ErrorCode,
                receiveErrorEventArgs.ApiRequestException.Message
            );
        }
    }
}
