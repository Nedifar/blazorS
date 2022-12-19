using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TabloBlazorMain.Shared.AdditionalLessonsModels
{
    public class SheduleAdditionalLesson
    {
        [Key]
        public int idSheduleAdditionalLesson { get; set; }

        public string name { get; set; }

        public int durationLesson { get; set; }

        public virtual List<DayWeek> DayWeeks { get; set; } = new List<DayWeek>();

        public virtual List<Time> Times { get; set; } = new List<Time>();

    }

    public class AddSheduleAdditionalLessonModel
    {
        public string name { get; set; }

        public int durationLesson { get; set; }


        public List<DayChecked> values { get; set; } = new List<DayChecked>()
        {
            new DayChecked{ name = "Понедельник", Checked= false },
            new DayChecked{ name = "Вторник",Checked= false },
            new DayChecked{ name = "Среда",Checked= false },
            new DayChecked{ name = "Четверг",Checked= false },
            new DayChecked{ name = "Пятница", Checked=false },
            new DayChecked{ name = "Суббота",Checked= false },
            new DayChecked{ name = "Воскресенье", Checked=false }
        };
    }

    public class DayChecked
    {
        public string name { get; set; }

        public bool Checked { get; set; }
    }
}
