using ClosedXML.Excel;
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
using TabloBlazorMain.Shared.LastDanceModels;
using TabloBlazorMain.Shared.Models;

namespace TabloBlazorMain.Server.LastDanceHostedServices
{
    public class FloorSheduleLoadHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private IMemoryCache cache;
        private Context.context context;
        public FloorSheduleLoadHostedService(IServiceScopeFactory serviceScopeFactory, IMemoryCache cache)
        {
            _serviceScopeFactory = serviceScopeFactory;

            this.cache = cache;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var scope = _serviceScopeFactory.CreateScope();
                    var _terminalService = scope.ServiceProvider.GetRequiredService<Context.context>();
                    context = _terminalService;
                    await Task.Run(() =>
                    {
                        List<FloorCabinet> floorCabinets = new List<FloorCabinet>();
                        while ((List<string>)cache.Get("MainListCabinets") is null)
                        {

                        }
                        while ((IXLWorksheet)cache.Get("xLMain") is null)
                        {

                        }
                        while ((List<string>)cache.Get("MainListTeachers") is null)
                        {

                        }
                        var iX = (IXLWorksheet)cache.Get("xLMain");
                        List<string> cabinets = (List<string>)cache.Get("MainListCabinets");
                        List<string> teachers = (List<string>)cache.Get("MainListTeachers");

                        foreach (string cabinet in cabinets)
                        {
                            List<DayWeekClass> days = new List<DayWeekClass>();
                            int row = 6;
                            List<DayWeekClass> kabinets = Differents.raspisaniekab(row * (int)DateTime.Now.DayOfWeek, cabinet, iX);
                            kabinets.ForEach(kabinets =>
                            {
                                kabinets.teacherMobile = kabinets.teacher(teachers);
                            });
                            days.AddRange(kabinets.ToArray());
                            floorCabinets.Add(new FloorCabinet { DayWeeks = days, Name = cabinet });
                        }
                        List<Para> paras = new List<Para>();
                        var list = context.TimeShedules.ToList();
                        if (list.Where(p => p.Name == DateTime.Now.ToShortDateString()).Count() != 0)
                        {
                            paras = list.Where(p => p.Name == DateTime.Now.ToShortDateString()).FirstOrDefault().OnlyParas.OrderBy(p => p.numberInterval).ToList();
                        }
                        else if ((int)DateTime.Now.DayOfWeek == context.SpecialDayWeekNames.FirstOrDefault().dayWeek)
                        {
                            paras = list.Where(p => p.Name == "ЧКР").FirstOrDefault().OnlyParas.OrderBy(p => p.numberInterval).ToList();
                        }
                        else
                        {
                            paras = list.Where(p => p.Name == "Основной").FirstOrDefault().OnlyParas.OrderBy(p => p.numberInterval).ToList();
                        }

                        foreach (var item in context.Lessons.Where(p => p.DayWeek.idDayWeek == (int)DateTime.Now.DayOfWeek).ToList())
                        {
                            if (item.cabinet == null)
                                continue;

                            var cab = floorCabinets.Where(p => p.Name == item.cabinet).FirstOrDefault()?.DayWeeks;
                            if (cab == null)
                                continue;
                            for (int i = 0; i < paras.Count(); i++)
                            {
                                if (paras[i].end.TimeOfDay >= item.Time.beginTime.TimeOfDay)
                                {
                                    if (paras[i].outGraphicNewTablo == "ЧКР")
                                        continue;
                                    cab.Where(p => p.Number.ToString() == paras[i].outGraphicNewTablo).FirstOrDefault().Day = "Начало: " + item.Time.beginTime.ToString("HH:mm") + "\n" + item.Time.SheduleAdditionalLesson.name + "\n" + item.teacherName + "\n" + item.groupName;
                                    for (int j = i; j < paras.Count(); j++)
                                    {
                                        if (paras[j].begin.TimeOfDay < item.Time.beginTime.AddMinutes(item.Time.SheduleAdditionalLesson.durationLesson).TimeOfDay)
                                        {
                                            cab.Where(p => p.Number.ToString() == paras[j].outGraphicNewTablo).FirstOrDefault().Day = item.Time.SheduleAdditionalLesson.name + "\n" + item.teacherName + "\n" + item.groupName;
                                            if (i == j)
                                                cab.Where(p => p.Number.ToString() == paras[j].outGraphicNewTablo).FirstOrDefault().Day = "Начало: " + item.Time.beginTime.ToString("HH:mm") + "\n" + cab.Where(p => p.Number.ToString() == paras[j].outGraphicNewTablo).FirstOrDefault().Day;
                                            if (paras[j].end.TimeOfDay >= item.Time.beginTime.AddMinutes(item.Time.SheduleAdditionalLesson.durationLesson).TimeOfDay)
                                            {
                                                cab.Where(p => p.Number.ToString() == paras[j].outGraphicNewTablo).FirstOrDefault().Day += "\n" + "Конец: " + item.Time.beginTime.AddMinutes(item.Time.SheduleAdditionalLesson.durationLesson).ToString("HH:mm");
                                            }
                                        }
                                    }
                                    break;
                                }
                            }

                        }

                        for (int i = 0; i < floorCabinets.Count; i++)
                        {
                            int l = 1;
                            if (i is 0)
                                l--;
                            for (int j = 0; j < floorCabinets[i].DayWeeks.Count(); j++)
                            {
                                if (floorCabinets[i].DayWeeks[j].decipline.Trim().Length <= 2 || floorCabinets[i].DayWeeks[j].decipline == "ЧКР")
                                {
                                    floorCabinets[i].DayWeeks[j].pp = "Пусто";
                                    if (j + 1 == floorCabinets[i].DayWeeks.Count())
                                        break;
                                    if (floorCabinets[i].DayWeeks[j + 1].Number == l + 1 || floorCabinets[i].DayWeeks[j].Number == null)
                                    {
                                        if (floorCabinets[i].DayWeeks[j + 1].decipline.Trim().Length <= 2)
                                            floorCabinets[i].DayWeeks[j].pp = "Пусто";
                                        else
                                        {
                                            floorCabinets[i].DayWeeks[j].pp = "Есть следующее занятие";
                                            floorCabinets[i].DayWeeks[j].gr1 = $"Группа: {floorCabinets[i].DayWeeks[j + 1].group}";
                                            floorCabinets[i].DayWeeks[j].dec1 = $"Дисциплина: {floorCabinets[i].DayWeeks[j + 1].decipline}";
                                        }
                                    }
                                    if (floorCabinets[i].DayWeeks[j + 1].Number != l + 1 && floorCabinets[i].DayWeeks[j + 1].Number != null)
                                    {
                                        l = 1;
                                        continue;
                                    }
                                    if (floorCabinets[i].DayWeeks[j + 1].Number == null) l--;
                                    l++;
                                }
                                else
                                {
                                    floorCabinets[i].DayWeeks[j].pp = "Есть";
                                    if (j + 1 == floorCabinets[i].DayWeeks.Count())
                                        break;
                                    if (floorCabinets[i].DayWeeks[j + 1].Number == l + 1 || floorCabinets[i].DayWeeks[j + 1].Number == null)
                                    {
                                        if (floorCabinets[i].DayWeeks[j + 1].decipline.Trim().Length <= 2 || floorCabinets[i].DayWeeks[j + 1].decipline == "ЧКР")
                                            floorCabinets[i].DayWeeks[j].pp = "Дальше пусто";
                                        else
                                        {
                                            floorCabinets[i].DayWeeks[j].pp = $"Есть следующее занятие";
                                            floorCabinets[i].DayWeeks[j].gr1 = $"Группа: {floorCabinets[i].DayWeeks[j + 1].group}";
                                            floorCabinets[i].DayWeeks[j].dec1 = $"Дисциплина: {floorCabinets[i].DayWeeks[j + 1].decipline}";
                                        }
                                    }
                                    if (floorCabinets[i].DayWeeks[j + 1].Number != l + 1 && floorCabinets[i].DayWeeks[j + 1].Number != null)
                                    {
                                        l = 1;
                                        continue;
                                    }
                                    if (floorCabinets[i].DayWeeks[j + 1].Number == null) l--;
                                    l++;
                                }
                            }
                        }
                        for (int i = 0; i < floorCabinets.Count; i++)
                        {
                            for (int j = 0; j < floorCabinets[i].DayWeeks.Count(); j++)
                            {
                                if (floorCabinets[i].DayWeeks[j].pp.Contains("Сегодня кабинет свободен"))
                                { continue; }

                                if (!floorCabinets[i].DayWeeks[j].pp.Contains("Есть"))
                                {
                                    string str = floorCabinets[i].DayWeeks[j].Number.ToString();
                                    bool b = true;
                                    int g = 1;
                                    while (str != "6")
                                    {
                                        if (floorCabinets[i].DayWeeks[j + g].pp.Contains("Есть"))
                                        {
                                            floorCabinets[i].DayWeeks[j].pp = "Нет следующего занятия, но кабинет будет захвачен позже";
                                            b = false;
                                            break;
                                        }
                                        str = floorCabinets[i].DayWeeks[j + g].Number.ToString();
                                        g++;
                                    }
                                    if (!b || floorCabinets[i].DayWeeks[j].Number != 1)
                                    { }
                                    else
                                    {
                                        str = floorCabinets[i].DayWeeks[j].Number.ToString();
                                        floorCabinets[i].DayWeeks[j].pp = "Сегодня кабинет свободен";
                                        g = 1;
                                        while (str != "6")
                                        {
                                            floorCabinets[i].DayWeeks[j + g].pp = "Сегодня кабинет свободен";
                                            str = floorCabinets[i].DayWeeks[j + g].Number.ToString();
                                            g++;
                                        }
                                    }
                                }
                                if (floorCabinets[i].DayWeeks[j].pp.Contains("Дальше пусто"))
                                { floorCabinets[i].DayWeeks[j].pp = "Последнее занятие, пожалуйста, поднимите за собой стулья."; }
                                if (floorCabinets[i].DayWeeks[j].pp == "Пусто")
                                { floorCabinets[i].DayWeeks[j].pp = "На сегодня занятия закончились."; }
                                if (floorCabinets[i].DayWeeks[j].pp == "Есть")
                                { floorCabinets[i].DayWeeks[j].pp = "Последнее занятие, пожалуйста, поднимите за собой стулья."; }
                            }
                        }
                        cache.Set("FullFloorShedule", floorCabinets);

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
                        stream.WriteLine(DateTime.Now + " ----- " + "FloorSheduleError" + ex.Message);
                        stream.Close();
                    }
                }
                await Task.Delay(10000 * 10);
            }
        }
    }
}
