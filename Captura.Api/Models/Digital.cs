using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Captura.Api.Models
{
    public class Digital
    {

        String digital_texto;
        byte[] digital_imagem_byte;

        public string Digital_texto { get => digital_texto; set => digital_texto = value; }
        public byte[] Digital_imagem_byte { get => digital_imagem_byte; set => digital_imagem_byte = value; }

    }
}