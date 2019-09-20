using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using NITGEN.SDK.NBioBSP;

namespace Captura.Api.Controllers
{

    [RoutePrefix("api/public/v1/captura")]
    public class CapturaController : ApiController
    {
        [HttpGet]
        [Route("Capturar/{id:int:min(1)}")]
        public Byte[] Capturar(int id)
        {
            NBioAPI m_NBioAPI = new NBioAPI();
            NBioAPI.Type.INIT_INFO_0 initInfo0;
            uint ret = m_NBioAPI.GetInitInfo(out initInfo0);
            Console.WriteLine("passeiaqui");
            if (ret == NBioAPI.Error.NONE)
            {
                initInfo0.EnrollImageQuality = Convert.ToUInt32(50);
                initInfo0.VerifyImageQuality = Convert.ToUInt32(30);
                initInfo0.DefaultTimeout = Convert.ToUInt32(10000);
                initInfo0.SecurityLevel = (int)NBioAPI.Type.FIR_SECURITY_LEVEL.NORMAL - 1;
            }

            NBioAPI.IndexSearch m_IndexSearch = new NBioAPI.IndexSearch(m_NBioAPI);
            NBioAPI.Type.HFIR hCapturedFIR;
            NBioAPI.Type.HFIR AuditData = new NBioAPI.Type.HFIR();
            NBioAPI.Type.FIR_TEXTENCODE texto;
            // Get FIR data
            m_NBioAPI.OpenDevice(NBioAPI.Type.DEVICE_ID.AUTO);
            m_NBioAPI.Capture(NBioAPI.Type.FIR_PURPOSE.DENTIFY, out hCapturedFIR, -1, AuditData, null);
            //Console.WriteLine(Encoding.Default.GetString(getfingerImage(AuditData)));

            try
            {
                if (hCapturedFIR != null)
                {
             
                    return getfingerImage(AuditData);
                }
                return null;
            }
            catch (Exception ex)
            {

                throw new Exception("ERRO:... " + ex.Message);
            }
        }



        [HttpGet]
        [Route("Enroll/{id:int:min(1)}")]
        public string Enroll(int id)
        {

            NBioAPI m_NBioAPI = new NBioAPI();
            NBioAPI.Type.FIR_TEXTENCODE m_textFIR;
            NBioAPI.Type.HFIR NewFIR;
            NBioAPI.IndexSearch m_IndexSearch = new NBioAPI.IndexSearch(m_NBioAPI);
            

            NBioAPI.Type.WINDOW_OPTION m_WinOption = new NBioAPI.Type.WINDOW_OPTION();
            m_WinOption.WindowStyle = (uint)NBioAPI.Type.WINDOW_STYLE.NO_WELCOME;

            string Retorno = "";

            m_NBioAPI.OpenDevice(NBioAPI.Type.DEVICE_ID.AUTO);
            uint ret = m_NBioAPI.Enroll(out NewFIR, null);

            //uint ret = m_NBioAPI.Enroll(null, out NewFIR, null, NBioAPI.Type.TIMEOUT.DEFAULT, null, m_WinOption);


            if (ret != NBioAPI.Error.NONE)
            {
                m_NBioAPI.CloseDevice(NBioAPI.Type.DEVICE_ID.AUTO);
            }

            if (NewFIR != null)
            {
                m_NBioAPI.GetTextFIRFromHandle(NewFIR, out m_textFIR, true);
                

                if (m_textFIR.TextFIR != null)
                {
                    m_NBioAPI.CloseDevice(NBioAPI.Type.DEVICE_ID.AUTO);
                    Retorno = m_textFIR.TextFIR.ToString();
                }
            }
            return Retorno;
        }


        [HttpGet]
        //[Route("Identificar/{id:int:min(1)}")]
        [Route("Identificar")]
        public string Identificar(string Digital)
        {

            NBioAPI m_NBioAPI = new NBioAPI();
            NBioAPI.Type.FIR_TEXTENCODE m_textFIR = new NBioAPI.Type.FIR_TEXTENCODE();
            //NBioAPI.Type.HFIR NewFIR;
            NBioAPI.IndexSearch m_IndexSearch = new NBioAPI.IndexSearch(m_NBioAPI);
            NBioAPI.Type.HFIR hCapturedFIR;
            NBioAPI.IndexSearch.FP_INFO[] fpInfo;
            

            NBioAPI.Type.WINDOW_OPTION m_WinOption = new NBioAPI.Type.WINDOW_OPTION();
            m_WinOption.WindowStyle = (uint)NBioAPI.Type.WINDOW_STYLE.NO_WELCOME;

            uint ID = 1;

            m_textFIR.TextFIR = Digital;
            m_IndexSearch.AddFIR(m_textFIR, ID, out fpInfo);

            uint dataCount;
            m_IndexSearch.GetDataCount(out dataCount);

            m_NBioAPI.OpenDevice(NBioAPI.Type.DEVICE_ID.AUTO);
            uint ret = m_NBioAPI.Capture(out hCapturedFIR);
            
            if (ret != NBioAPI.Error.NONE)
            {
                //DisplayErrorMsg(ret);
                m_NBioAPI.CloseDevice(NBioAPI.Type.DEVICE_ID.AUTO);
                m_NBioAPI.GetTextFIRFromHandle(hCapturedFIR, out m_textFIR, true);
            }

            m_NBioAPI.CloseDevice(NBioAPI.Type.DEVICE_ID.AUTO);


            NBioAPI.IndexSearch.FP_INFO fpInfo2;
          	NBioAPI.IndexSearch.CALLBACK_INFO_0 cbInfo0 = new NBioAPI.IndexSearch.CALLBACK_INFO_0();
			cbInfo0.CallBackFunction = new NBioAPI.IndexSearch.INDEXSEARCH_CALLBACK(myCallback);

            // Identify FIR to IndexSearch DB
            ret = m_IndexSearch.IdentifyData(hCapturedFIR, NBioAPI.Type.FIR_SECURITY_LEVEL.NORMAL, out fpInfo2, cbInfo0);
            if (ret != NBioAPI.Error.NONE)
            {
                //DisplayErrorMsg(ret);
                return fpInfo2.ID.ToString();

            }

            return "";
        }

        public uint myCallback(ref NBioAPI.IndexSearch.CALLBACK_PARAM_0 cbParam0, IntPtr userParam)
        {
            //progressIdentify.Value = Convert.ToInt32(cbParam0.ProgressPos);
            return NBioAPI.IndexSearch.CALLBACK_RETURN.OK;
        }


        [HttpGet]
        [Route("Comparar")]
        public string Comparar(string Digital)
        {
            string Retorno = CompararDigital(Digital);
            return Retorno;
        }


        private string CompararDigital(string Digital)
        {
            uint ret;
            bool result;
            NBioAPI m_NBioAPI = new NBioAPI();
            NBioAPI.Type.HFIR hCapturedFIR = new NBioAPI.Type.HFIR();
            NBioAPI.Type.FIR_TEXTENCODE m_textFIR = new NBioAPI.Type.FIR_TEXTENCODE();
            NBioAPI.Type.FIR_PAYLOAD myPayload = new NBioAPI.Type.FIR_PAYLOAD();

            m_textFIR.TextFIR = Digital.ToString();

            m_NBioAPI.OpenDevice(NBioAPI.Type.DEVICE_ID.AUTO);
            m_NBioAPI.Capture(out hCapturedFIR);

            ret = m_NBioAPI.VerifyMatch(hCapturedFIR, m_textFIR, out result, myPayload);


            if (result == true)
                return "OK";
            else
                return "";
        }

        public Byte[]getfingerImage(NBioAPI.Type.HFIR Fir_toImage)
        {
            NBioAPI m_NBioAPI = new NBioAPI();
            NBioAPI.Export.EXPORT_AUDIT_DATA exportData;
            NBioAPI.Export m_Export = new NBioAPI.Export(m_NBioAPI);

            m_Export.NBioBSPToImage(Fir_toImage, out exportData);
            Bitmap bmp;
            int nWidth = (int)exportData.ImageWidth;
            int nHeight = (int)exportData.ImageHeight;

            {     // bmp image save...
                bmp = new Bitmap(nWidth, nHeight, PixelFormat.Format8bppIndexed);
                BitmapData data = bmp.LockBits(new Rectangle(0, 0, nWidth, nHeight), ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

                System.Runtime.InteropServices.Marshal.Copy(exportData.AuditData[0].Image[0].Data, 0, data.Scan0, nWidth * nHeight);
                bmp.UnlockBits(data);
                ColorPalette GrayscalePalette = bmp.Palette;
                for (int i = 0; i < 256; i++)
                    GrayscalePalette.Entries[i] = Color.FromArgb(i, i, i);

                bmp.Palette = GrayscalePalette;

                // bmp.Save("RollImage.bmp");

            }

            //Image img = bmp;
            MemoryStream stream = new MemoryStream();
            bmp.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);
            Byte[] bytes = stream.ToArray();


            return bytes;
        }


    }
}
