using ClosedXML.Excel;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using TabloBlazorMain.Shared.LastDanceModels;
using TabloBlazorMain.Server.LastDanceResources;
using System.Text.RegularExpressions;
using TabloBlazorMain.Shared.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;

namespace TabloBlazorMain.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LastDanceController : ControllerBase
    {
        IMemoryCache cache;
        Context.context context;
        private readonly IHubContext<GoodHubGbl> _hubContext;
        public LastDanceController(IHubContext<GoodHubGbl> hubContext, Context.context _context, IMemoryCache cache)
        {
            _hubContext = hubContext;
            context = _context;
            this.cache = cache;
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("ru-RU");
        }

        [HttpGet("getnes")]
        public ActionResult<string> GetNew() //Метод для возвращения информации о наличии нового расписания
        {
            IXLWorksheet result = null;
            if (cache.TryGetValue<IXLWorksheet>("xLNew", out result))
            {
                if (result == null)
                {
                    return Ok("нет нового расписания");
                }
                else
                {
                    return Ok("есть новое расписание");
                }
            }
            else
            {
                return Ok("нет нового расписания");
            }
        }

        [HttpGet("getteacher/{teacher}")] //вернуть расписание по преподавателям
        public ActionResult GetTeach(string teacher)
        {
            IXLWorksheet result = null;
            List<FullDayWeekClass> fullDayWeekClasses = new List<FullDayWeekClass>();
            int row = 6;

            result = (IXLWorksheet)cache.Get("xLMain");

            List<DayWeekClass> teachers = Differents.raspisanieteach(row * (int)DateTime.UtcNow.AddHours(5).DayOfWeek, teacher, result);
            string day = CultureInfo.GetCultureInfo("ru-RU").DateTimeFormat.GetDayName(DateTime.UtcNow.AddHours(5).DayOfWeek);
            fullDayWeekClasses.Add(new FullDayWeekClass
            {
                dayWeekName = day.ToUpper()[0] + day.Substring(1),
                dayWeekClasses = teachers
            });
            List<Para> paras = new List<Para>();
            var list = context.TimeShedules.ToList();
            if (list.Where(p => p.Name == DateTime.Now.ToShortDateString()).Count() != 0)
            {
                paras = list.Where(p => p.Name == DateTime.Now.ToShortDateString()).FirstOrDefault().OnlyParas.OrderBy(p => p.numberInterval).ToList();
            }
            else if ((int)DateTime.Now.DayOfWeek == context.SpecialDayWeekNames.FirstOrDefault().dayWeek)
            {
                paras = list.Where(p => p.Name == "ЧКР").FirstOrDefault().OnlyParas.OrderBy(p => p.numberInterval).ToList();
                for (int i = 0; i < paras.Count; i++)
                {
                    if (paras[i].outGraphicNewTablo == "ЧКР")
                        fullDayWeekClasses.FirstOrDefault()?.dayWeekClasses.Insert(i, new DayWeekClass { Day = "ЧКР" });
                }
            }
            else
            {
                paras = list.Where(p => p.Name == "Основной").FirstOrDefault().OnlyParas.OrderBy(p => p.numberInterval).ToList();
            }


            foreach (var item in context.Lessons.Where(p => p.teacherName == teacher && (int)DateTime.UtcNow.AddHours(5).DayOfWeek == p.idDayWeek).ToList())
            {
                var cab = fullDayWeekClasses.FirstOrDefault().dayWeekClasses;
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
            return Ok(fullDayWeekClasses);
        }

        [HttpGet]
        [Route("getTeacherMobile/{teacher}")]
        public ActionResult GetTeacherMobile(string teacher, DateTime date)
        {
            IXLWorksheet result = null;
            List<FullDayWeekClass> fullDayWeekClasses = new List<FullDayWeekClass>();

            while (result is null)
            {
                if (DateTime.UtcNow.AddHours(5).Date == date.Date)
                {
                    result = (IXLWorksheet)cache.Get("xLMain");
                    for (int j = 1; j <= 6; j++)
                    {
                        List<DayWeekClass> metrics = Differents.raspisanieteach(j * (int)DateTime.UtcNow.AddHours(5).DayOfWeek, teacher, result);
                        string day = CultureInfo.GetCultureInfo("ru-RU").DateTimeFormat.GetDayName(new DateTime(2022, 12, 11).AddDays(j).DayOfWeek);
                        fullDayWeekClasses.Add(new FullDayWeekClass
                        {
                            dayWeekName = day.ToUpper()[0] + day.Substring(1),
                            dayWeekClasses = metrics
                        });

                        List<Para> paras = new List<Para>();
                        Differents dif = new();
                        dif.DateOut(DateTime.Now);
                        var list = context.TimeShedules.ToList();
                        if (list.Where(p => p.Name == dif.DdownDay.AddDays(j - 1).ToShortDateString()).Count() != 0)
                        {
                            paras = list.Where(p => p.Name == DateTime.Now.ToShortDateString()).FirstOrDefault().OnlyParas.OrderBy(p => p.numberInterval).ToList();
                        }
                        else if ((int)dif.DdownDay.AddDays(j - 1).DayOfWeek == context.SpecialDayWeekNames.FirstOrDefault().dayWeek)
                        {
                            paras = list.Where(p => p.Name == "ЧКР").FirstOrDefault().OnlyParas.OrderBy(p => p.numberInterval).ToList();
                            for (int i = 0; i < paras.Count; i++)
                            {
                                if (paras[i].outGraphicNewTablo == "ЧКР")
                                    fullDayWeekClasses.FirstOrDefault()?.dayWeekClasses.Insert(i, new DayWeekClass { Day = "ЧКР" });
                            }
                        }
                        else
                        {
                            paras = list.Where(p => p.Name == "Основной").FirstOrDefault().OnlyParas.OrderBy(p => p.numberInterval).ToList();
                        }
                        var cab = fullDayWeekClasses[j - 1].dayWeekClasses;
                        var teachers = (List<string>)cache.Get("MainListTeachers");
                        for (int i = 0; i < cab.Count(); i++)
                        {
                            cab[i].beginMobile = paras[i].begin.ToShortTimeString();
                            cab[i].endMobile = paras[i].end.ToShortTimeString();
                            cab[i].teacherMobile = cab[i].teacher(teachers);
                            cab[i].Date = dif.DdownDay.AddDays(j - 1);
                        }
                        foreach (var item in context.Lessons.Where(p => p.teacherName == teacher&& (int)DateTime.UtcNow.AddHours(5).DayOfWeek == p.idDayWeek).ToList())
                        {
                            for (int i = 0; i < paras.Count(); i++)
                            {
                                if (paras[i].end.TimeOfDay >= item.Time.beginTime.TimeOfDay)
                                {
                                    if (paras[i].outGraphicNewTablo == "ЧКР")
                                        continue;
                                    cab.Where(p => p.Number.ToString() == paras[i].outGraphicNewTablo).FirstOrDefault().Day = "Начало: " + item.Time.beginTime.ToString("HH:mm") + "\n" + item.Time.SheduleAdditionalLesson.name + "\n" + item.teacherName + "\n" + item.groupName;
                                    for (int l = i; l < paras.Count(); l++)
                                    {
                                        if (paras[l].begin.TimeOfDay < item.Time.beginTime.AddMinutes(item.Time.SheduleAdditionalLesson.durationLesson).TimeOfDay)
                                        {
                                            cab.Where(p => p.Number.ToString() == paras[l].outGraphicNewTablo).FirstOrDefault().Day = item.Time.SheduleAdditionalLesson.name + "\n" + item.teacherName + "\n" + item.groupName;
                                            if (i == l)
                                                cab.Where(p => p.Number.ToString() == paras[l].outGraphicNewTablo).FirstOrDefault().Day = "Начало: " + item.Time.beginTime.ToString("HH:mm") + "\n" + cab.Where(p => p.Number.ToString() == paras[l].outGraphicNewTablo).FirstOrDefault().Day;
                                            if (paras[l].end.TimeOfDay >= item.Time.beginTime.AddMinutes(item.Time.SheduleAdditionalLesson.durationLesson).TimeOfDay)
                                            {
                                                cab.Where(p => p.Number.ToString() == paras[l].outGraphicNewTablo).FirstOrDefault().Day += "\n" + "Конец: " + item.Time.beginTime.AddMinutes(item.Time.SheduleAdditionalLesson.durationLesson).ToString("HH:mm");
                                            }
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                    }

                }
                else if (DateTime.UtcNow.AddHours(5).Date.AddDays(7) == date.Date)
                {
                    result = (IXLWorksheet)cache.Get("xLNew");
                    for (int j = 1; j <= 6; j++)
                    {
                        List<DayWeekClass> metrics = Differents.raspisanieteach(j * (int)DateTime.UtcNow.AddHours(5).DayOfWeek, teacher, result);
                        string day = CultureInfo.GetCultureInfo("ru-RU").DateTimeFormat.GetDayName(new DateTime(2022, 12, 11).AddDays(j).DayOfWeek);
                        fullDayWeekClasses.Add(new FullDayWeekClass
                        {
                            dayWeekName = day.ToUpper()[0] + day.Substring(1),
                            dayWeekClasses = metrics
                        });

                        List<Para> paras = new List<Para>();
                        Differents dif = new();
                        dif.DateOut(date);
                        var list = context.TimeShedules.ToList();
                        if (list.Where(p => p.Name == dif.DdownDay.AddDays(j - 1).ToShortDateString()).Count() != 0)
                        {
                            paras = list.Where(p => p.Name == DateTime.Now.ToShortDateString()).FirstOrDefault().OnlyParas.OrderBy(p => p.numberInterval).ToList();
                        }
                        else if ((int)dif.DdownDay.AddDays(j - 1).DayOfWeek == context.SpecialDayWeekNames.FirstOrDefault().dayWeek)
                        {
                            paras = list.Where(p => p.Name == "ЧКР").FirstOrDefault().OnlyParas.OrderBy(p => p.numberInterval).ToList();
                            for (int i = 0; i < paras.Count; i++)
                            {
                                if (paras[i].outGraphicNewTablo == "ЧКР")
                                    fullDayWeekClasses.FirstOrDefault()?.dayWeekClasses.Insert(i, new DayWeekClass { Day = "ЧКР" });
                            }
                        }
                        else
                        {
                            paras = list.Where(p => p.Name == "Основной").FirstOrDefault().OnlyParas.OrderBy(p => p.numberInterval).ToList();
                        }
                        var cab = fullDayWeekClasses[j - 1].dayWeekClasses;
                        var teachers = (List<string>)cache.Get("MainListTeachers");
                        for (int i = 0; i < cab.Count(); i++)
                        {
                            cab[i].beginMobile = paras[i].begin.ToShortTimeString();
                            cab[i].endMobile = paras[i].end.ToShortTimeString();
                            cab[i].teacherMobile = cab[i].teacher(teachers);
                            cab[i].Date = dif.DdownDay.AddDays(j - 1);
                        }
                        foreach (var item in context.Lessons.Where(p => p.teacherName == teacher && (int)DateTime.UtcNow.AddHours(5).DayOfWeek == p.idDayWeek).ToList())
                        {
                            for (int i = 0; i < paras.Count(); i++)
                            {
                                if (paras[i].end.TimeOfDay >= item.Time.beginTime.TimeOfDay)
                                {
                                    if (paras[i].outGraphicNewTablo == "ЧКР")
                                        continue;
                                    cab.Where(p => p.Number.ToString() == paras[i].outGraphicNewTablo).FirstOrDefault().Day = "Начало: " + item.Time.beginTime.ToString("HH:mm") + "\n" + item.Time.SheduleAdditionalLesson.name + "\n" + item.teacherName + "\n" + item.groupName;
                                    for (int l = i; l < paras.Count(); l++)
                                    {
                                        if (paras[l].begin.TimeOfDay < item.Time.beginTime.AddMinutes(item.Time.SheduleAdditionalLesson.durationLesson).TimeOfDay)
                                        {
                                            cab.Where(p => p.Number.ToString() == paras[l].outGraphicNewTablo).FirstOrDefault().Day = item.Time.SheduleAdditionalLesson.name + "\n" + item.teacherName + "\n" + item.groupName;
                                            if (i == l)
                                                cab.Where(p => p.Number.ToString() == paras[l].outGraphicNewTablo).FirstOrDefault().Day = "Начало: " + item.Time.beginTime.ToString("HH:mm") + "\n" + cab.Where(p => p.Number.ToString() == paras[l].outGraphicNewTablo).FirstOrDefault().Day;
                                            if (paras[l].end.TimeOfDay >= item.Time.beginTime.AddMinutes(item.Time.SheduleAdditionalLesson.durationLesson).TimeOfDay)
                                            {
                                                cab.Where(p => p.Number.ToString() == paras[l].outGraphicNewTablo).FirstOrDefault().Day += "\n" + "Конец: " + item.Time.beginTime.AddMinutes(item.Time.SheduleAdditionalLesson.durationLesson).ToString("HH:mm");
                                            }
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    //result = Differents.dictSpecial[date.Date].Item1;
                    //days.Add(new DayWeekClass { Day = "ЧКР" });
                    //int column = Differents.IndexGroup(group, result);
                    //for (int j = 1; j <= 6; j++)
                    //{
                    //    List<DayWeekClass> metrics = Differents.EnumerableMetrics(j * 6, column, result);
                    //    days.AddRange(metrics.ToArray());
                    //}
                }
            }
            return Ok(fullDayWeekClasses);
        }

        [HttpGet("getgroup/{group}")]
        public ActionResult Get(string group) //вернуть расписание по группам
        {
            IXLWorksheet result = null;
            List<FullDayWeekClass> fullDayWeekClasses = new List<FullDayWeekClass>();
            result = (IXLWorksheet)cache.Get("xLMain");
            int column = Differents.IndexGroup(group, result);
            List<DayWeekClass> metrics = Differents.EnumerableMetrics((int)DateTime.UtcNow.AddHours(5).DayOfWeek * 6, column, result);
            string day = CultureInfo.GetCultureInfo("ru-RU").DateTimeFormat.GetDayName(DateTime.UtcNow.AddHours(5).DayOfWeek);
            fullDayWeekClasses.Add(new FullDayWeekClass
            {
                dayWeekName = day.ToUpper()[0] + day.Substring(1),
                dayWeekClasses = metrics
            });
            List<Para> paras = new List<Para>();
            var list = context.TimeShedules.ToList();
            if (list.Where(p => p.Name == DateTime.Now.ToShortDateString()).Count() != 0)
            {
                paras = list.Where(p => p.Name == DateTime.Now.ToShortDateString()).FirstOrDefault().OnlyParas.OrderBy(p => p.numberInterval).ToList();
            }
            else if ((int)DateTime.Now.DayOfWeek == context.SpecialDayWeekNames.FirstOrDefault().dayWeek)
            {
                paras = list.Where(p => p.Name == "ЧКР").FirstOrDefault().OnlyParas.OrderBy(p => p.numberInterval).ToList();
                for (int i = 0; i < paras.Count; i++)
                {
                    if (paras[i].outGraphicNewTablo == "ЧКР")
                        fullDayWeekClasses.FirstOrDefault()?.dayWeekClasses.Insert(i, new DayWeekClass { Day = "ЧКР" });
                }
            }
            else
            {
                paras = list.Where(p => p.Name == "Основной").FirstOrDefault().OnlyParas.OrderBy(p => p.numberInterval).ToList();
            }


            foreach (var item in context.Lessons.Where(p => p.groupName == group && (int)DateTime.UtcNow.AddHours(5).DayOfWeek == p.idDayWeek).ToList())
            {
                var cab = fullDayWeekClasses.FirstOrDefault().dayWeekClasses;
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
            return Ok(fullDayWeekClasses);
        }

        [HttpGet("getgroupMobile")]
        public ActionResult GetGroupMobile(string group, DateTime date)
        {
            IXLWorksheet result = null;
            List<FullDayWeekClass> fullDayWeekClasses = new List<FullDayWeekClass>();

            while (result is null)
            {
                if (DateTime.UtcNow.AddHours(5).Date == date.Date)
                {
                    result = (IXLWorksheet)cache.Get("xLMain");
                    int column = Differents.IndexGroup(group, result);
                    for (int j = 1; j <= 6; j++)
                    {
                        List<DayWeekClass> metrics = Differents.EnumerableMetrics(j * 6, column, result);
                        string day = CultureInfo.GetCultureInfo("ru-RU").DateTimeFormat.GetDayName(new DateTime(2022, 12, 11).AddDays(j).DayOfWeek);
                        fullDayWeekClasses.Add(new FullDayWeekClass
                        {
                            dayWeekName = day.ToUpper()[0] + day.Substring(1),
                            dayWeekClasses = metrics
                        });

                        List<Para> paras = new List<Para>();
                        Differents dif = new();
                        dif.DateOut(DateTime.Now);
                        var list = context.TimeShedules.ToList();
                        if (list.Where(p => p.Name == dif.DdownDay.AddDays(j - 1).ToShortDateString()).Count() != 0)
                        {
                            paras = list.Where(p => p.Name == dif.DdownDay.AddDays(j - 1).ToShortDateString()).FirstOrDefault().OnlyParas.OrderBy(p => p.numberInterval).ToList();
                        }
                        else if ((int)dif.DdownDay.AddDays(j - 1).DayOfWeek == context.SpecialDayWeekNames.FirstOrDefault().dayWeek)
                        {
                            paras = list.Where(p => p.Name == "ЧКР").FirstOrDefault().OnlyParas.OrderBy(p => p.numberInterval).ToList();
                            for (int i = 0; i < paras.Count; i++)
                            {
                                if (paras[i].outGraphicNewTablo == "ЧКР")
                                    fullDayWeekClasses.FirstOrDefault()?.dayWeekClasses.Insert(i, new DayWeekClass { Day = "ЧКР" });
                            }
                        }
                        else
                        {
                            paras = list.Where(p => p.Name == "Основной").FirstOrDefault().OnlyParas.OrderBy(p => p.numberInterval).ToList();
                        }
                        var cab = fullDayWeekClasses[j - 1].dayWeekClasses;
                        var teachers = (List<string>)cache.Get("MainListTeachers");
                        for (int i = 0; i < cab.Count(); i++)
                        {
                            cab[i].beginMobile = paras[i].begin.ToShortTimeString();
                            cab[i].endMobile = paras[i].end.ToShortTimeString();
                            cab[i].teacherMobile = cab[i].teacher(teachers);
                            cab[i].Date = dif.DdownDay.AddDays(j - 1);
                        }
                        foreach (var item in context.Lessons.Where(p => p.groupName == group && (int)(new DateTime(2022, 12, 11).AddDays(j).DayOfWeek) == p.idDayWeek).ToList())
                        {

                            for (int i = 0; i < paras.Count(); i++)
                            {
                                if (paras[i].end.TimeOfDay >= item.Time.beginTime.TimeOfDay)
                                {
                                    if (paras[i].outGraphicNewTablo == "ЧКР")
                                        continue;
                                    cab.Where(p => p.Number.ToString() == paras[i].outGraphicNewTablo).FirstOrDefault().Day = "Начало: " + item.Time.beginTime.ToString("HH:mm") + "\n" + item.Time.SheduleAdditionalLesson.name + "\n" + item.teacherName + "\n" + item.groupName;
                                    for (int l = i; l < paras.Count(); l++)
                                    {
                                        if (paras[l].begin.TimeOfDay < item.Time.beginTime.AddMinutes(item.Time.SheduleAdditionalLesson.durationLesson).TimeOfDay)
                                        {
                                            cab.Where(p => p.Number.ToString() == paras[l].outGraphicNewTablo).FirstOrDefault().Day = item.Time.SheduleAdditionalLesson.name + "\n" + item.teacherName + "\n" + item.groupName;
                                            if (i == l)
                                                cab.Where(p => p.Number.ToString() == paras[l].outGraphicNewTablo).FirstOrDefault().Day = "Начало: " + item.Time.beginTime.ToString("HH:mm") + "\n" + cab.Where(p => p.Number.ToString() == paras[l].outGraphicNewTablo).FirstOrDefault().Day;
                                            if (paras[l].end.TimeOfDay >= item.Time.beginTime.AddMinutes(item.Time.SheduleAdditionalLesson.durationLesson).TimeOfDay)
                                            {
                                                cab.Where(p => p.Number.ToString() == paras[l].outGraphicNewTablo).FirstOrDefault().Day += "\n" + "Конец: " + item.Time.beginTime.AddMinutes(item.Time.SheduleAdditionalLesson.durationLesson).ToString("HH:mm");
                                            }
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }
                else if (DateTime.UtcNow.AddMinutes(5).Date.AddDays(7) == date.Date)
                {
                    result = (IXLWorksheet)cache.Get("xLNew");
                    if (result == null)
                    {
                        return NotFound("Расписание для данной недели не найдено. Повторить поиск?");
                    }
                    int column = Differents.IndexGroup(group, result);
                    if(column == 0)
                    {
                        return NotFound("Расписания для данной группы не найдено. Повторить поиск?");
                    }
                    for (int j = 1; j <= 6; j++)
                    {
                        List<DayWeekClass> metrics = Differents.EnumerableMetrics(j * 6, column, result);
                        string day = CultureInfo.GetCultureInfo("ru-RU").DateTimeFormat.GetDayName(new DateTime(2022, 12, 11).AddDays(j).DayOfWeek);
                        fullDayWeekClasses.Add(new FullDayWeekClass
                        {
                            dayWeekName = day.ToUpper()[0] + day.Substring(1),
                            dayWeekClasses = metrics
                        });

                        List<Para> paras = new List<Para>();
                        Differents dif = new();
                        dif.DateOut(date);
                        var list = context.TimeShedules.ToList();
                        if (list.Where(p => p.Name == dif.DdownDay.AddDays(j - 1).ToShortDateString()).Count() != 0)
                        {
                            paras = list.Where(p => p.Name == dif.DdownDay.AddDays(j - 1).ToShortDateString()).FirstOrDefault().OnlyParas.OrderBy(p => p.numberInterval).ToList();
                        }
                        else if ((int)dif.DdownDay.AddDays(j - 1).DayOfWeek == context.SpecialDayWeekNames.FirstOrDefault().dayWeek)
                        {
                            paras = list.Where(p => p.Name == "ЧКР").FirstOrDefault().OnlyParas.OrderBy(p => p.numberInterval).ToList();
                            for (int i = 0; i < paras.Count; i++)
                            {
                                if (paras[i].outGraphicNewTablo == "ЧКР")
                                    fullDayWeekClasses.FirstOrDefault()?.dayWeekClasses.Insert(i, new DayWeekClass { Day = "ЧКР" });
                            }
                        }
                        else
                        {
                            paras = list.Where(p => p.Name == "Основной").FirstOrDefault().OnlyParas.OrderBy(p => p.numberInterval).ToList();
                        }
                        var cab = fullDayWeekClasses[j - 1].dayWeekClasses;
                        var teachers = (List<string>)cache.Get("NewListTeachers");
                        for (int i = 0; i < cab.Count(); i++)
                        {
                            cab[i].beginMobile = paras[i].begin.ToShortTimeString();
                            cab[i].endMobile = paras[i].end.ToShortTimeString();
                            cab[i].teacherMobile = cab[i].teacher(teachers);
                            cab[i].Date = dif.DdownDay.AddDays(j - 1);
                        }
                        foreach (var item in context.Lessons.Where(p => p.groupName == group && (int)(new DateTime(2022, 12, 11).AddDays(j).DayOfWeek) == p.idDayWeek).ToList())
                        {
                            for (int i = 0; i < paras.Count(); i++)
                            {
                                if (paras[i].end.TimeOfDay >= item.Time.beginTime.TimeOfDay)
                                {
                                    if (paras[i].outGraphicNewTablo == "ЧКР")
                                        continue;
                                    cab.Where(p => p.Number.ToString() == paras[i].outGraphicNewTablo).FirstOrDefault().Day = "Начало: " + item.Time.beginTime.ToString("HH:mm") + "\n" + item.Time.SheduleAdditionalLesson.name + "\n" + item.teacherName + "\n" + item.groupName;
                                    for (int l = i; l < paras.Count(); l++)
                                    {
                                        if (paras[l].begin.TimeOfDay < item.Time.beginTime.AddMinutes(item.Time.SheduleAdditionalLesson.durationLesson).TimeOfDay)
                                        {
                                            cab.Where(p => p.Number.ToString() == paras[l].outGraphicNewTablo).FirstOrDefault().Day = item.Time.SheduleAdditionalLesson.name + "\n" + item.teacherName + "\n" + item.groupName;
                                            if (i == l)
                                                cab.Where(p => p.Number.ToString() == paras[l].outGraphicNewTablo).FirstOrDefault().Day = "Начало: " + item.Time.beginTime.ToString("HH:mm") + "\n" + cab.Where(p => p.Number.ToString() == paras[l].outGraphicNewTablo).FirstOrDefault().Day;
                                            if (paras[l].end.TimeOfDay >= item.Time.beginTime.AddMinutes(item.Time.SheduleAdditionalLesson.durationLesson).TimeOfDay)
                                            {
                                                cab.Where(p => p.Number.ToString() == paras[l].outGraphicNewTablo).FirstOrDefault().Day += "\n" + "Конец: " + item.Time.beginTime.AddMinutes(item.Time.SheduleAdditionalLesson.durationLesson).ToString("HH:mm");
                                            }
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    try
                    {


                        try
                        {
                            result = Differents.dictSpecial[date].Item1;
                        }
                        catch
                        {
                            if (Differents.SpecialSheduleReturn(date) == null)
                            {
                                return NotFound("Расписание для данной недели не найдено. Повторить поиск?");
                            }
                            result = Differents.dictSpecial[date].Item1;
                        }
                        if (result == null)
                        {
                            return NotFound("Расписание для данной недели не найдено. Повторить поиск?");
                        }
                        int column = Differents.IndexGroup(group, result);
                        if (column == 0)
                        {
                            return NotFound("Расписания для данной группы не найдено. Повторить поиск?");
                        }
                        for (int j = 1; j <= 6; j++)
                        {
                            List<DayWeekClass> metrics = Differents.EnumerableMetrics(j * 6, column, result);
                            string day = CultureInfo.GetCultureInfo("ru-RU").DateTimeFormat.GetDayName(new DateTime(2022, 12, 11).AddDays(j).DayOfWeek);
                            fullDayWeekClasses.Add(new FullDayWeekClass
                            {
                                dayWeekName = day.ToUpper()[0] + day.Substring(1),
                                dayWeekClasses = metrics
                            });

                            List<Para> paras = new List<Para>();
                            Differents dif = new();
                            dif.DateOut(date);
                            var list = context.TimeShedules.ToList();
                            if (list.Where(p => p.Name == dif.DdownDay.AddDays(j - 1).ToShortDateString()).Count() != 0)
                            {
                                paras = list.Where(p => p.Name == dif.DdownDay.AddDays(j - 1).ToShortDateString()).FirstOrDefault().OnlyParas.OrderBy(p => p.numberInterval).ToList();
                            }
                            else if ((int)dif.DdownDay.AddDays(j - 1).DayOfWeek == context.SpecialDayWeekNames.FirstOrDefault().dayWeek)
                            {
                                paras = list.Where(p => p.Name == "ЧКР").FirstOrDefault().OnlyParas.OrderBy(p => p.numberInterval).ToList();
                                for (int i = 0; i < paras.Count; i++)
                                {
                                    if (paras[i].outGraphicNewTablo == "ЧКР")
                                        fullDayWeekClasses.FirstOrDefault()?.dayWeekClasses.Insert(i, new DayWeekClass { Day = "ЧКР" });
                                }
                            }
                            else
                            {
                                paras = list.Where(p => p.Name == "Основной").FirstOrDefault().OnlyParas.OrderBy(p => p.numberInterval).ToList();
                            }
                            var cab = fullDayWeekClasses[j - 1].dayWeekClasses;
                            var teachers = (List<string>)cache.Get("MainListTeachers");
                            for (int i = 0; i < cab.Count(); i++)
                            {
                                cab[i].beginMobile = paras[i].begin.ToShortTimeString();
                                cab[i].endMobile = paras[i].end.ToShortTimeString();
                                cab[i].teacherMobile = cab[i].teacher(teachers);
                                cab[i].Date = dif.DdownDay.AddDays(j - 1);
                            }
                            foreach (var item in context.Lessons.Where(p => p.groupName == group && (int)(new DateTime(2022, 12, 11).AddDays(j).DayOfWeek) == p.idDayWeek).ToList())
                            {

                                for (int i = 0; i < paras.Count(); i++)
                                {
                                    if (paras[i].end.TimeOfDay >= item.Time.beginTime.TimeOfDay)
                                    {
                                        if (paras[i].outGraphicNewTablo == "ЧКР")
                                            continue;
                                        cab.Where(p => p.Number.ToString() == paras[i].outGraphicNewTablo).FirstOrDefault().Day = "Начало: " + item.Time.beginTime.ToString("HH:mm") + "\n" + item.Time.SheduleAdditionalLesson.name + "\n" + item.teacherName + "\n" + item.groupName;
                                        for (int l = i; l < paras.Count(); l++)
                                        {
                                            if (paras[l].begin.TimeOfDay < item.Time.beginTime.AddMinutes(item.Time.SheduleAdditionalLesson.durationLesson).TimeOfDay)
                                            {
                                                cab.Where(p => p.Number.ToString() == paras[l].outGraphicNewTablo).FirstOrDefault().Day = item.Time.SheduleAdditionalLesson.name + "\n" + item.teacherName + "\n" + item.groupName;
                                                if (i == l)
                                                    cab.Where(p => p.Number.ToString() == paras[l].outGraphicNewTablo).FirstOrDefault().Day = "Начало: " + item.Time.beginTime.ToString("HH:mm") + "\n" + cab.Where(p => p.Number.ToString() == paras[l].outGraphicNewTablo).FirstOrDefault().Day;
                                                if (paras[l].end.TimeOfDay >= item.Time.beginTime.AddMinutes(item.Time.SheduleAdditionalLesson.durationLesson).TimeOfDay)
                                                {
                                                    cab.Where(p => p.Number.ToString() == paras[l].outGraphicNewTablo).FirstOrDefault().Day += "\n" + "Конец: " + item.Time.beginTime.AddMinutes(item.Time.SheduleAdditionalLesson.durationLesson).ToString("HH:mm");
                                                }
                                            }
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    catch(Exception ex)
                    {

                    }
                }
            }
            return Ok(fullDayWeekClasses);
        }

        [HttpGet("getcabinet/{cabinet}")] //вернуть расписание по кабинетам
        public ActionResult GetKab(string cabinet)
        {
            IXLWorksheet result = null;
            List<FullDayWeekClass> fullDayWeekClasses = new List<FullDayWeekClass>();
            result = (IXLWorksheet)cache.Get("xLMain");
            //days.Add(new DayWeekClass { Day = "ЧКР" }); ?????!!!!
            int row = 6;
            List<DayWeekClass> сabinets = Differents.raspisaniekab(row * (int)DateTime.UtcNow.AddHours(5).DayOfWeek, cabinet, result);
            string day = CultureInfo.GetCultureInfo("ru-RU").DateTimeFormat.GetDayName(DateTime.UtcNow.AddHours(5).DayOfWeek);
            fullDayWeekClasses.Add(new FullDayWeekClass
            {
                dayWeekName = day.ToUpper()[0] + day.Substring(1),
                dayWeekClasses = сabinets

            });
            List<Para> paras = new List<Para>();
            var list = context.TimeShedules.ToList();
            if (list.Where(p => p.Name == DateTime.Now.ToShortDateString()).Count() != 0)
            {
                paras = list.Where(p => p.Name == DateTime.Now.ToShortDateString()).FirstOrDefault().OnlyParas.OrderBy(p => p.numberInterval).ToList();
            }
            else if ((int)DateTime.Now.DayOfWeek == context.SpecialDayWeekNames.FirstOrDefault().dayWeek)
            {
                paras = list.Where(p => p.Name == "ЧКР").FirstOrDefault().OnlyParas.OrderBy(p => p.numberInterval).ToList();
                for (int i = 0; i < paras.Count; i++)
                {
                    if (paras[i].outGraphicNewTablo == "ЧКР")
                        fullDayWeekClasses.FirstOrDefault()?.dayWeekClasses.Insert(i, new DayWeekClass { Day = "ЧКР" });
                }
            }
            else
            {
                paras = list.Where(p => p.Name == "Основной").FirstOrDefault().OnlyParas.OrderBy(p => p.numberInterval).ToList();
            }


            foreach (var item in context.Lessons.Where(p => p.cabinet == cabinet && (int)DateTime.UtcNow.AddHours(5).DayOfWeek == p.idDayWeek).ToList())
            {
                var cab = fullDayWeekClasses.FirstOrDefault().dayWeekClasses;
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
            return Ok(fullDayWeekClasses);
        }

        [HttpGet]
        [Route("getcabinetMobile/{cabinet}")]
        public ActionResult GetCabinetMobile(string cabinet, DateTime date)
        {
            IXLWorksheet result = null;
            List<FullDayWeekClass> fullDayWeekClasses = new List<FullDayWeekClass>();

            while (result is null)
            {
                if (DateTime.UtcNow.AddHours(5).Date == date.Date)
                {
                    result = (IXLWorksheet)cache.Get("xLMain");
                    for (int j = 1; j <= 6; j++)
                    {
                        List<DayWeekClass> metrics = Differents.raspisaniekab(j * (int)DateTime.UtcNow.AddHours(5).DayOfWeek, cabinet, result);
                        string day = CultureInfo.GetCultureInfo("ru-RU").DateTimeFormat.GetDayName(new DateTime(2022, 12, 11).AddDays(j).DayOfWeek);
                        fullDayWeekClasses.Add(new FullDayWeekClass
                        {
                            dayWeekName = day.ToUpper()[0] + day.Substring(1),
                            dayWeekClasses = metrics
                        });

                        List<Para> paras = new List<Para>();
                        Differents dif = new();
                        dif.DateOut(DateTime.Now);
                        var list = context.TimeShedules.ToList();
                        if (list.Where(p => p.Name == dif.DdownDay.AddDays(j - 1).ToShortDateString()).Count() != 0)
                        {
                            paras = list.Where(p => p.Name == DateTime.Now.ToShortDateString()).FirstOrDefault().OnlyParas.OrderBy(p => p.numberInterval).ToList();
                        }
                        else if ((int)dif.DdownDay.AddDays(j - 1).DayOfWeek == context.SpecialDayWeekNames.FirstOrDefault().dayWeek)
                        {
                            paras = list.Where(p => p.Name == "ЧКР").FirstOrDefault().OnlyParas.OrderBy(p => p.numberInterval).ToList();
                            for (int i = 0; i < paras.Count; i++)
                            {
                                if (paras[i].outGraphicNewTablo == "ЧКР")
                                    fullDayWeekClasses.FirstOrDefault()?.dayWeekClasses.Insert(i, new DayWeekClass { Day = "ЧКР" });
                            }
                        }
                        else
                        {
                            paras = list.Where(p => p.Name == "Основной").FirstOrDefault().OnlyParas.OrderBy(p => p.numberInterval).ToList();
                        }
                        var cab = fullDayWeekClasses[j - 1].dayWeekClasses;
                        var teachers = (List<string>)cache.Get("MainListTeachers");
                        for (int i = 0; i < cab.Count(); i++)
                        {
                            cab[i].beginMobile = paras[i].begin.ToShortTimeString();
                            cab[i].endMobile = paras[i].end.ToShortTimeString();
                            cab[i].teacherMobile = cab[i].teacher(teachers);
                            cab[i].Date = dif.DdownDay.AddDays(j - 1);
                        }
                        foreach (var item in context.Lessons.Where(p => p.cabinet == cabinet && (int)DateTime.UtcNow.AddHours(5).DayOfWeek == p.idDayWeek).ToList())
                        {
                            for (int i = 0; i < paras.Count(); i++)
                            {
                                if (paras[i].end.TimeOfDay >= item.Time.beginTime.TimeOfDay)
                                {
                                    if (paras[i].outGraphicNewTablo == "ЧКР")
                                        continue;
                                    cab.Where(p => p.Number.ToString() == paras[i].outGraphicNewTablo).FirstOrDefault().Day = "Начало: " + item.Time.beginTime.ToString("HH:mm") + "\n" + item.Time.SheduleAdditionalLesson.name + "\n" + item.teacherName + "\n" + item.groupName;
                                    for (int l = i; l < paras.Count(); l++)
                                    {
                                        if (paras[l].begin.TimeOfDay < item.Time.beginTime.AddMinutes(item.Time.SheduleAdditionalLesson.durationLesson).TimeOfDay)
                                        {
                                            cab.Where(p => p.Number.ToString() == paras[l].outGraphicNewTablo).FirstOrDefault().Day = item.Time.SheduleAdditionalLesson.name + "\n" + item.teacherName + "\n" + item.groupName;
                                            if (i == l)
                                                cab.Where(p => p.Number.ToString() == paras[l].outGraphicNewTablo).FirstOrDefault().Day = "Начало: " + item.Time.beginTime.ToString("HH:mm") + "\n" + cab.Where(p => p.Number.ToString() == paras[l].outGraphicNewTablo).FirstOrDefault().Day;
                                            if (paras[l].end.TimeOfDay >= item.Time.beginTime.AddMinutes(item.Time.SheduleAdditionalLesson.durationLesson).TimeOfDay)
                                            {
                                                cab.Where(p => p.Number.ToString() == paras[l].outGraphicNewTablo).FirstOrDefault().Day += "\n" + "Конец: " + item.Time.beginTime.AddMinutes(item.Time.SheduleAdditionalLesson.durationLesson).ToString("HH:mm");
                                            }
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                    }

                }
                else if (DateTime.UtcNow.AddMinutes(5).Date.AddDays(7) == date.Date)
                {
                    //result = (IXLWorksheet)cache.Get("xLNew");
                    //for (int j = 1; j <= 6; j++)
                    //{
                    //    List<DayWeekClass> metrics = Differents.EnumerableMetrics(j * 6, column, result);
                    //    string day = CultureInfo.GetCultureInfo("ru-RU").DateTimeFormat.GetDayName(new DateTime(11, 12, 2022).AddDays(j).DayOfWeek);
                    //    fullDayWeekClasses.Add(new FullDayWeekClass
                    //    {
                    //        dayWeekName = day.ToUpper()[0] + day.Substring(1),
                    //        dayWeekClasses = metrics
                    //    });
                    //}
                }
                else
                {
                    //result = Differents.dictSpecial[date.Date].Item1;
                    //days.Add(new DayWeekClass { Day = "ЧКР" });
                    //int column = Differents.IndexGroup(group, result);
                    //for (int j = 1; j <= 6; j++)
                    //{
                    //    List<DayWeekClass> metrics = Differents.EnumerableMetrics(j * 6, column, result);
                    //    days.AddRange(metrics.ToArray());
                    //}
                }
            }
            return Ok(fullDayWeekClasses);
        }

        [HttpGet]
        [Route("getcabinetsList")]
        public ActionResult<IEnumerable<List<string>>> Get() //вернуть список кабиентов
        {
            while ((List<string>)cache.Get("MainListCabinets") is null)
            {

            }
            return Ok((List<string>)cache.Get("MainListCabinets"));
        }
        [HttpGet]
        [Route("getteachersList")]
        public ActionResult<IEnumerable<List<string>>> GetTeachList() //вернуть список преподавателей
        {
            while ((List<string>)cache.Get("MainListTeachers") is null)
            {

            }
            return Ok((List<string>)cache.Get("MainListTeachers"));
        }

        [HttpGet]
        [Route("getgroupList")]
        public ActionResult<IEnumerable<List<string>>> get(DateTime date) //вернуть список групп
        {
            var listResult = new List<string>();
            if(DateTime.UtcNow.AddHours(5).Date == date)
            {
                while (listResult.Count() == 0)
                {
                    listResult = (List<string>)cache.Get("MainListGroups");
                }
            }
            else if (DateTime.UtcNow.AddHours(5).Date.AddDays(7) == date)
            {
                while (listResult.Count() == 0)
                {
                    listResult = (List<string>)cache.Get("NewListGroups");
                    if(listResult == null)
                    {
                        return Ok(new List<string>());
                    }
                }
            }
            else
            {
                var lp = Differents.SpecialSheduleReturn(date);
                if (lp == null)
                    return Ok(null);
                listResult = lp.Groups;
                while (lp.Groups ==null)
                {
                    listResult = lp.Groups;
                }
            }

            return Ok(listResult);
        }

        public static void RaspisanieIzm(XLWorkbook _workbook1, int h, IXLWorksheet ix) //Метод для перезаписи в расписании изменений
        {
            DateTime dateIZM = DateTime.Today;
            int dayWeek = (int)DateTime.Today.DayOfWeek;
            var worksheet = _workbook1.Worksheets.First();
            for (int i = 1; i <= worksheet.ColumnsUsed().Count(); i++)
            {
                int n = worksheet.RowsUsed().Count();
                for (int j = 11; j <= worksheet.RowsUsed().Count() + 10; j++)
                {
                    for (int l = 3; l <= ix.ColumnsUsed().Count(); l++)
                    {
                        if (ix.Cell(5, l).GetValue<string>() == worksheet.Cell(j, i).GetValue<string>())
                        {
                            bool a = false;
                            int g = 6;
                            for (int m = 1; m <= g; m++)
                            {
                                IXLCell leg = worksheet.Cell(j + m, i);
                                if (leg.Style.Font.FontSize >= 22 || leg.Value.ToString() == "" || a || leg.Value.ToString().Length == 4)
                                {
                                    if (ix.Cell(27, 2).Value.ToString() != "4")
                                        ix.Cell((6 * h) + m, l).Value = " ";
                                    else
                                        ix.Cell((6 * h) + m - 1, l).Value = " ";
                                    a = true;
                                }
                                else
                                {
                                    if (ix.Cell(27, 2).Value.ToString() != "4")
                                        ix.Cell((6 * h) + m, l).Value = worksheet.Cell(j + m, i);
                                    else
                                        ix.Cell((6 * h) + m - 1, l).Value = worksheet.Cell(j + m, i);
                                }
                            }
                        }
                    }
                }
            }
        }

        [HttpPost]
        [Route("getFloorShedule")]
        public ActionResult GetFlooreShedule(PostFloorModel models)
        {
            try
            {
                List<DayWeekClass> weekClasses = new List<DayWeekClass>();
                while ((List<string>)cache.Get("MainListCabinets") is null)
                {

                }
                while ((IXLWorksheet)cache.Get("xLMain") is null)
                {

                }
                while ((List<FloorCabinet>)cache.Get("FullFloorShedule") is null)
                {

                }
                var iX = (IXLWorksheet)cache.Get("xLMain");
                List<string> cabinets = (List<string>)cache.Get("MainListCabinets");
                List<FloorCabinet> floorsCabinets = (List<FloorCabinet>)cache.Get("FullFloorShedule");
                List<string> filtredCabinets = new List<string>();
                Regex regex;
                switch (models.floor)
                {
                    case "11":
                        regex = new Regex("^1[0-9]{2}");
                        break;
                    case "12":
                        regex = new Regex("^1[0-9]{1}[^0-9]{0,1}$");
                        break;
                    case "21":
                        regex = new Regex("^2[0-9]{2}");
                        break;
                    case "22":
                        regex = new Regex("^2[0-9]{1}[^0-9]{0,1}$");
                        break;
                    case "31":
                        regex = new Regex("^3[0-9]{2}");
                        break;
                    case "32":
                        regex = new Regex("^3[0-9]{1}[^0-9]{0,1}$");
                        break;
                    case "41":
                        regex = new Regex("^4[0-9]{2}");
                        break;
                    case "42":
                        regex = new Regex("^4[0-9]{1}[^0-9]{0,1}$");
                        break;
                    default:
                        regex = new Regex("[0-9]{2,3}");
                        break;
                }
                if (int.TryParse(models.paraNow, out int numberPara) || models.paraNow == "ЧКР")
                {
                    try
                    {
                        foreach (string cabinet in cabinets)
                        {
                            if (regex.IsMatch(cabinet))
                            {
                                var psevdores = floorsCabinets.Where(p => p.Name == cabinet).FirstOrDefault();
                                if (models.CHKR.Where(p => p.TypeInterval.name == "ЧКР").FirstOrDefault() != null)
                                {
                                    var item = new Shared.LastDanceModels.DayWeekClass { Day = "ЧКР" };
                                    var usl = models.CHKR.Where(p => p.TypeInterval.name == "ЧКР" || p.TypeInterval.name == "Пара").ToList();
                                    for (int i = 0; i < usl.Count(); i++)
                                    {
                                        if (usl[i].TypeInterval.name == "ЧКР")
                                        {
                                            if (i == 0)
                                            {
                                                item.cabinet = cabinet;
                                                item.pp = "Сейчас идёт ЧКР";
                                            }
                                            else
                                            {
                                                item.pp = weekClasses[i - 1].pp;
                                                psevdores.DayWeeks[i - 1].pp = "Сейчас будет ЧКР\n" + psevdores.DayWeeks[i - 1].pp;
                                            }
                                            psevdores.DayWeeks.Insert(i, item);
                                            break;
                                        }
                                    }
                                }
                                if (models.paraNow == "ЧКР") { weekClasses.Add(psevdores.DayWeeks.Where(p => p.Number == null).FirstOrDefault()); }
                                else { weekClasses.Add(psevdores.DayWeeks.Where(p => p.Number == numberPara).FirstOrDefault()); }
                            }
                        }
                    }
                    catch { }
                }


                else if (models.paraNow is null)
                {
                    foreach (string cabinet in cabinets)
                    {
                        if (regex.IsMatch(cabinet))
                        {
                            weekClasses.Add(new DayWeekClass { cabinet = cabinet });
                        }
                    }
                }

                return Ok(weekClasses);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("GetCabinentsWithDetail")]
        public ActionResult GetCabinentsWithDetail(string cabinet)
        {
            List<DayWeekClass> weekClasses = new List<DayWeekClass>();
            while ((List<string>)cache.Get("MainListCabinets") is null)
            {

            }
            while ((IXLWorksheet)cache.Get("xLMain") is null)
            {

            }
            while ((List<FloorCabinet>)cache.Get("FullFloorShedule") is null)
            {

            }
            var iX = (IXLWorksheet)cache.Get("xLMain");
            List<FloorCabinet> floorsCabinets = (List<FloorCabinet>)cache.Get("FullFloorShedule");
            var resultWeekClass = new List<DayWeekClass>();
            resultWeekClass = floorsCabinets.Where(p => p.Name == cabinet).FirstOrDefault()
                ?.DayWeeks.ToList();
            return Ok(resultWeekClass);
        }

        [HttpGet("update")] //запрос для обновления данных между сервером и объявлениями
        public async Task<ActionResult> PostUpdateChangesAnnouncment()
        {
            var list = context.Announcements.ToList();
            var linka = new List<Announcement>();
            int i = 1;
            foreach (var lili in list.Where(p => p.dateAdded < DateTime.Now && (p.dateClosed >= DateTime.Now || p.dateClosed == null) && p.isActive).OrderByDescending(p => p.Priority).ThenByDescending(p => p.idAnnouncement).Take(5))
            {
                linka.Add(lili);
                linka.LastOrDefault().idAnnouncement = i;
                i++;
            }
            cache.Set("ann", linka, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) });
            using (var stream = new StreamWriter("log.txt", true))
            {
                stream.WriteLine(DateTime.Now + " ----- " + $"SendAnn");
                stream.Close();
            }
            await _hubContext.Clients.All.SendAsync("SendAnn", linka);
            return Ok();
        }
    }
}