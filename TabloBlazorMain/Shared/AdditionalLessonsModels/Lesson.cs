using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TabloBlazorMain.Shared.AdditionalLessonsModels
{
    public class Lesson
    {
        [Key]
        public int idLesson { get; set; }

        public string groupName { get; set; }

        public string teacherName { get; set; }

        public string cabinet { get; set; }

        [ForeignKey("DayWeek")]
        public int idDayWeek { get; set; }

        public virtual DayWeek DayWeek { get; set; }

        [ForeignKey("Time")]
        public int idTime { get; set; }

        public virtual Time Time { get; set; }

        public string getGroupTeacherCell
        {
            get
            {
                string resultString = "";
                if(groupName != null)
                {
                    resultString += groupName;
                }
                if (cabinet != null)
                {
                    resultString += " " +cabinet;
                }
                if (teacherName != null)
                {
                    resultString += "\n" + teacherName;
                }
                
                return resultString;
            }
        }
    }
}
