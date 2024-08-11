using System;
using System.Text.RegularExpressions;

namespace DonBot.Utilities
{
    public class DataValidation
    {

        public string ValidatePhone(string text)
        {
            string pattern = @"^\d+$";
            if(Regex.IsMatch(text, pattern))
            {
                return "";
            }
            else
            {
                return "Numero de celular incorrecto. Recuerda no usar caracteres especiales o letras";
            }
        }

        public string ValidateDate (string date)
        {
            string pattern = @"^\d{4}-\d{2}-\d{2}$";
            if (Regex.IsMatch(date, pattern))
            {
                // Intentar analizar la fecha para asegurar que sea válida
                if (DateTime.TryParseExact(date, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out _))
                {
                    return "";
                }
            }

            return "Formato de fecha incorrecto. Recuerda colocar una fecha valida en el formato AAAA-MM-DD. Ejemplo: 1999-02-13";
        }

        public string ValidateEmail(string text)
        {
            string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            if (Regex.IsMatch(text, pattern))
            {
                return "";
            }

            return "Formato de correo electronico incorrecto. Recuerda escribir un correo valido. Ejemplo: example@correo.com";
        }
    }
}
