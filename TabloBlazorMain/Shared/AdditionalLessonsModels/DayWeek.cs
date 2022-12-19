using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TabloBlazorMain.Shared.AdditionalLessonsModels
{
    public class DayWeek
    {
        [Key]
        public int idDayWeek { get; set; }

        public string name { get; set; }

        public virtual List<SheduleAdditionalLesson> SheduleAdditionalLessons { get; set; } = new();

        public virtual List<Lesson> Lessons { get; set; } = new();

        
    }
}
