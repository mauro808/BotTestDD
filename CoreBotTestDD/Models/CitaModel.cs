using System.Data;

namespace CoreBotTestDD.Models
{
    public class CitaModel
    {
        public string appointmentTypeID { get; set; }

        public int userID { get; set; }

        public int DoctorID { get; set; }

        public int dateTime { get; set; }

        public int officeID { get; set; }  

        public int duration { get; set; }

    }
}
