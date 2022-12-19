using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using TabloBlazorMain.Server.LastDanceResources;
using TabloBlazorMain.Server.PhoneTask;
using TabloBlazorMain.Shared.Models;

namespace TabloBlazorMain.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TabloApiController : ControllerBase
    {
        IMemoryCache cache;
        Context.context context;
        public TabloApiController(Context.context _context, IMemoryCache cache)
        {
            context = _context;
            this.cache = cache;
        }
        [HttpGet("weekName")]
        public string getWeekName() //возвращает нижнюю верхнюю неделю
        {
            return cache.Get("weekName").ToString();
        }
        [HttpGet("weather")]
        public ActionResult<string> getWeather() //вернуть погоду
        {
            while (cache.Get("weather") == null)
            { }
            return cache.Get("weather").ToString();
        }
        [HttpGet("admins")]
        public string getAdministrtorName() //вернуть админа этой недели
        {

            while (cache.Get("admin") == null)
            { }
            return cache.Get("admin").ToString();
        }

        [HttpGet("DayPartHeader")]
        public ActionResult getDayPartHeader() //вернуть заголовок для текущего времени дня
        {
            while (cache.Get("dayPart") == null)
            { }
            return Ok(cache.Get("dayPart").ToString());
        }

        [HttpGet("announc")]
        public List<Announcement> getAnnouncment() //вернуть все объявления
        {
            List<Announcement> linka;
            if (!cache.TryGetValue("ann", out linka))
            {
                var list = context.Announcements.ToList();
                linka = new List<Announcement>();
                int i = 1;
                foreach (var lili in list.Where(p => p.dateAdded < DateTime.Now && (p.dateClosed >= DateTime.Now || p.dateClosed == null) && p.isActive).OrderByDescending(p => p.Priority).ThenByDescending(p => p.idAnnouncement).Take(5))
                {
                    linka.Add(lili);
                    linka.LastOrDefault().idAnnouncement = i;
                    i++;
                }
                cache.Set("ann", linka, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60) });
            }
            return cache.Get("ann") as List<Announcement>;
        }
        [HttpGet("update")]
        public async Task<RequestForDynamicUpdate> DynamicUpdate() //обновить всю страницу
        {
            if (GlobalObjects.request == null)
            {
                await AsyncGetMethods.GetUpdate(context);
            }
            return GlobalObjects.request;
        }
    }
}
