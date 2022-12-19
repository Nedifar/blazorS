﻿using ClosedXML.Excel;
using HtmlAgilityPack;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TabloBlazorMain.Server.LastDanceResources;

namespace TabloBlazorMain.Server.LastDanceHostedServices
{
    public class MainSheduleHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private IMemoryCache cache;
        public MainSheduleHostedService(IServiceScopeFactory serviceScopeFactory, IMemoryCache cache)
        {
            _serviceScopeFactory = serviceScopeFactory;
            var scope = serviceScopeFactory.CreateScope();

            this.cache = cache;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    DateTime time = DateTime.UtcNow.AddHours(5);
                    Differents dif = new Differents();
                    dif.DateOut(time);
                    XLWorkbook xL = null;
                    string trim1 = dif.upMonth.Substring(0, 3);
                    HtmlDocument doc = new HtmlDocument();
                    var web1 = new HtmlWeb();
                    doc = web1.Load("https://oksei.ru/studentu/raspisanie_uchebnykh_zanyatij");
                    if (System.IO.File.Exists(@$"{AppDomain.CurrentDomain.BaseDirectory}Raspisanie/_{dif.downDay}_{dif.upDay}_{trim1}.xlsx"))
                    {
                        xL = new XLWorkbook(@$"{AppDomain.CurrentDomain.BaseDirectory}Raspisanie/_{dif.downDay}_{dif.upDay}_{trim1}.xlsx");

                    }
                    else
                    {
                        var nodes = doc.DocumentNode.SelectNodes("//*[@class='container bg-white p-25 box-shadow-right radius mt50']/div/div/p/a");
                        foreach (var node in nodes.Reverse())
                        {
                            if (node.InnerText.Contains("верх") || node.InnerText.Contains("нижн"))
                            {
#pragma warning disable SYSLIB0014 // Тип или член устарел
                                WebClient web = new();
#pragma warning restore SYSLIB0014 // Тип или член устарел
                                var href = node.Attributes["href"].Value;
                                var value = node.InnerText;
                                web.DownloadFile($"https://oksei.ru{href}", $"{AppDomain.CurrentDomain.BaseDirectory}Raspisanie/_{dif.DdownDay.Day}_{dif.DupDay.Day}_{dif.upMonth.Substring(0, 3)}.xls");
                                var workbook = new Aspose.Cells.Workbook(@$"{AppDomain.CurrentDomain.BaseDirectory}Raspisanie/_{dif.downDay}_{dif.upDay}_{dif.upMonth.Substring(0, 3)}.xls");
                                workbook.Save(@$"{AppDomain.CurrentDomain.BaseDirectory}Raspisanie/_{dif.downDay}_{dif.upDay}_{dif.upMonth.Substring(0, 3)}.xlsx", Aspose.Cells.SaveFormat.Xlsx);
                                xL = new XLWorkbook(@$"{AppDomain.CurrentDomain.BaseDirectory}Raspisanie/_{dif.downDay}_{dif.upDay}_{dif.upMonth.Substring(0, 3)}.xlsx");
                                cache.Set("xLMain", xL.Worksheets.First(), new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });
                                using (var streame = new StreamWriter($"{AppDomain.CurrentDomain.BaseDirectory}Raspisanie/Formats.txt", false))
                                {
                                    streame.Write(value);
                                    streame.Close();
                                }
                                break;
                            }
                        }
                    }
                    IXLWorksheet workSheet = xL.Worksheets.First();
                    Differents.DownloadFeatures(time, workSheet);
                    xL.Save();
                    cache.Set("xLMain", workSheet);
                    await Task.Run(() =>
                    {
                        int columnsCount = workSheet.ColumnsUsed().Count();
                        int rowsCount = workSheet.RowsUsed().Count();
                        bool exit = true;
                        List<string> dataforcb = new List<string>();
                        for (int i = 3; i <= columnsCount; i++)
                        {
                            for (int j = 6; j <= rowsCount; j++)
                            {
                                string result = workSheet.Cell(j, i).GetValue<string>();
                                if (result != "" && result != " " && result.Length > 3)
                                {
                                    result = result.Remove(0, result.Length - 3).Trim();
                                    if (result != "-" && result != "" && result.Length > 1)
                                    {
                                        Regex regex = new Regex("(^[0-9]{3}$)|(^[0-9]{2}[а-яА-Я]{0,1}$)");
                                        if (regex.IsMatch(result))
                                        {
                                            foreach (string output in dataforcb)
                                            {
                                                if (output == result)
                                                {
                                                    exit = false;
                                                    break;
                                                }
                                            }
                                            if (exit)
                                            {
                                                dataforcb.Add(result);
                                            }
                                            exit = true;
                                        }
                                    }
                                }
                            }
                        }
                        dataforcb.Sort();
                        cache.Set("MainListCabinets", dataforcb );
                    }); //список кабинетов
                    await Task.Run(() =>
                    {
                        bool exit = true;
                        int columnsCount = workSheet.ColumnsUsed().Count();
                        int rowsCount = workSheet.RowsUsed().Count();
                        List<string> dataforcb = new List<string>();
                        for (int i = 3; i <= columnsCount; i++)
                        {
                            for (int j = 6; j <= rowsCount; j++)
                            {
                                string result = workSheet.Cell(j, i).GetValue<string>();
                                if (result != "" && result != " " && result.Length > 3)
                                {
                                    if (result.Contains("ДОП"))
                                    {
                                        string[] massiv = result.Split(new char[] { '(', ')' });
                                        result = massiv[1].Trim();
                                    }
                                    else
                                    {
                                        try
                                        {
                                            if (result.Length == 4)
                                            {
                                                continue;
                                            }
                                            string[] massiv = result.Split('\n');
                                            if (massiv.Length == 1)
                                            { continue; }
                                            result = massiv[1].Trim();
                                        }
                                        catch
                                        { continue; }
                                    }
                                    if (result != "-" && result != "")
                                    {
                                        Regex regex = new Regex(@"[а-яА-Я]+\s[А-Я]{1}\.[А-Я]{1}\.?$");
                                        if (regex.IsMatch(result))
                                        {
                                            foreach (string output in dataforcb)
                                            {
                                                if (output == result)
                                                {
                                                    exit = false;
                                                    break;
                                                }
                                            }
                                            if (exit)
                                            {
                                                dataforcb.Add(result);
                                            }
                                            exit = true;
                                        }
                                    }
                                }
                            }
                        }
                        dataforcb.Sort();
                        cache.Set("MainListTeachers", dataforcb);
                    });
                    await Task.Run(() =>
                    {
                        List<string> dataforcb = new List<string>();
                        for (int i = 3; i <= workSheet.ColumnsUsed().Count(); i++)
                        {
                            dataforcb.Add(workSheet.Cell(5, i).GetValue<string>());
                        }
                        dataforcb.Sort();
                        cache.Set("MainListGroups", dataforcb);
                    });
                }
                catch (Exception ex)
                {
                    if (!File.Exists("log.txt"))
                    {
                        File.Create("log.txt");
                    }
                    using (var stream = new StreamWriter("log.txt", true))
                    {
                        stream.WriteLine(DateTime.Now + " ----- " + "MainSheduleError" + ex.Message);
                        stream.Close();
                    }
                }
                await Task.Delay(10000 * 10);
            }
        }
    }
}
