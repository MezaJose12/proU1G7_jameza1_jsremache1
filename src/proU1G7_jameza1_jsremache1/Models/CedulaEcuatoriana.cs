namespace proU1G7_jameza1_jsremache1.Models
{
    public static class CedulaEcuatoriana
    {
        public static bool EsValida(string numero)
        {
            if (string.IsNullOrWhiteSpace(numero) || numero.Length != 10)
            {
                return false;
            }

            for (var i = 0; i < numero.Length; i++)
            {
                if (numero[i] < '0' || numero[i] > '9')
                {
                    return false;
                }
            }

            var provincia = (numero[0] - '0') * 10 + numero[1] - '0';
            if (provincia < 1 || provincia > 24 || numero[2] - '0' > 5)
            {
                return false;
            }

            var suma = 0;
            for (var i = 0; i < 9; i++)
            {
                var digito = numero[i] - '0';
                if (i % 2 == 0)
                {
                    digito *= 2;
                    if (digito > 9)
                    {
                        digito -= 9;
                    }
                }
                suma += digito;
            }

            var digitoVerificador = (10 - suma % 10) % 10;
            return digitoVerificador == numero[9] - '0';
        }
    }
}
