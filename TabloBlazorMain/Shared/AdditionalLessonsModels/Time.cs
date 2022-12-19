using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TabloBlazorMain.Shared.AdditionalLessonsModels
{
    public class Time
    {
        [Key]
        public int idTime { get; set; }

        public DateTime beginTime { get; set; } = DateTime.Now;

        public virtual List<Lesson> Lessons { get; set; } = new();

        [NotMapped]
        public List<Lesson> GetLessonsSorted
        {
            get
            {
                return Lessons.OrderBy(p => p.idDayWeek).ToList();
            }
        }

        [ForeignKey("SheduleAdditionalLesson")]
        public int idSheduleAdditionalLesson { get; set; }

        public virtual SheduleAdditionalLesson SheduleAdditionalLesson { get; set; }

        public string getTime
        {
            get
            {
                return beginTime.ToString("HH.mm");
            }
        }
    }
}
