using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace multicast2unicast
{
    public class rtp
    {

        //buffer with data
        public byte[] buf; //data to process
        static int len; //size of the buffer
        public bool RTP_Process_Done; //Just someting that RTP_Process can write to if it is successful
        //public int RTPOK = -1;
        //byte[] pbuf; //only RTP Payload


        int is_rtp; //variable to set to 1 if data is an RTP packet

        public rtp(byte[] buffer, int lenght)
        {
            buf = buffer;
            len = lenght;
        }
        public rtp()
        {
        }
        //buffer with data

        //int hdrlen = 0;

        const byte MPEG_TS_SIG = 0x47;
        const int RTP_MIN_SIZE = 4;
        const int RTP_HDR_SIZE = 12; /* RFC 3550 */
        const byte RTP_VER2 = 0x02;

        /* offset to header extension and extension length,
            * as per RFC 3550 5.3.1 */
        const int XTLEN_OFFSET = 14;
        const int XTSIZE = 4;

        /* minimum length to determine size of an extended RTP header
         */
        int RTP_XTHDRLEN = (XTLEN_OFFSET + XTSIZE);

        const int CSRC_SIZE = 4;

        /* MPEG payload-type constants - adopted from VLC 0.8.6 */
        const byte P_MPGA = 0x0E;     /* MPEG audio */
        const byte P_MPGV = 0x20;     /* MPEG video */
        const byte P_MPGTS = 0x21;    /* MPEG TS    */


        public bool RTP_check()
        {
            //int error = 9;
            rtp _rtp = new rtp();
            /* check if the buffer is an RTP packet
            *
            * @param buf       buffer to analyze
            * @len             size of the buffer
            * @param is_rtp    variable to set to 1 if data is an RTP packet
             *
            * @return True if there was no error, False otherwise
            */

            int rtp_version = 0;
            int rtp_payload_type = 0;

            if (len < RTP_MIN_SIZE)
            {
                Console.WriteLine("RTP_check: buffer size {0} is less than minimum {1}\n", (long)len, (long)RTP_MIN_SIZE);
                return false;
            }
            /* initial assumption: is NOT RTP */
            _rtp.is_rtp = 0;

            if (MPEG_TS_SIG == buf[0])
            {
                Console.WriteLine("MPEG-TS stream detected\n");
                return true;
            }

            rtp_version = (buf[0] & 0xC0) >> 6;
            Debug.WriteLine("RTP Version: " + rtp_version);
            if (RTP_VER2 != rtp_version)
            {
                Console.WriteLine("RTP_check: wrong RTP version {0} must be {1}\n", (int)rtp_version, (int)RTP_VER2);
                return false;

            }

            if (len < RTP_HDR_SIZE)
            {
                Console.WriteLine("RTP_check: header size is too small {0}\n", (long)len);
                return false;
            }

            _rtp.is_rtp = 1;

            rtp_payload_type = (buf[1] & 0x7F);
            Debug.WriteLine("RTP Payload Type: " + rtp_payload_type);
            switch (rtp_payload_type)
            {
                case P_MPGA:
                    Console.WriteLine("RTP_check: {0} MPEG audio stream\n", (int)rtp_payload_type);
                    break;
                case P_MPGV:
                    Console.WriteLine("RTP_check: {0} MPEG video stream\n", (int)rtp_payload_type);
                    break;
                case P_MPGTS:
                    Console.WriteLine("RTP_check: {0} MPEG TS stream\n", (int)rtp_payload_type);
                    break;

                default:
                    Console.WriteLine("RTP_check: unsupported RTP payload type {0}\n", (int)rtp_payload_type);
                    return false;
            }
            return true;
        }


        public bool RTP_verify()
        {
            /* verify if buffer contains an RTP packet, 0 otherwise
            *
            * @param buf       buffer to analyze
            * @param len       size of the buffer
            *
            * @return True if buffer contains an RTP packet, False otherwise
            */
            int rtp_version = 0;
            int rtp_payload_type = 0;

            if ((len < RTP_MIN_SIZE) || (len < RTP_HDR_SIZE))
            {
                Console.WriteLine("RTP_verify: inappropriate size={0} of RTP packet\n", (long)len);
                return false;
            }

            rtp_version = (buf[0] & 0xC0) >> 6;
            //rtp_payload_type = (RTPPayloadTypes)(buf[1] & 0x7F);
            rtp_payload_type = buf[1] & 0x7F;

            /*
            TRACE( (void)tmfprintf( log, "RTP version [%d] at [%p]\n", rtp_version,
                        (const void*)buf ) );
            */

            if (RTP_VER2 != rtp_version)
            {
                /*
                TRACE( (void)tmfprintf( log, "RTP_verify: wrong RTP version [%d], "
                            "must be [%d]\n", (int)rtp_version, (int)RTP_VER2) );
                */
                return false;
            }

            switch (rtp_payload_type)
            {
                case P_MPGA:
                case P_MPGV:
                case P_MPGTS:
                    break;
                default:
                    Console.WriteLine("RTP_verify: unsupported RTP payload type {0}\n", (int)rtp_payload_type);
                    return false;
            }

            return true;
        }

        int? RTP_hdrlen()
        {
            /* calculate length of an RTP header
            *
            * @param buf       buffer to analyze
            * @param len       size of the buffer
            *
            * @return          RTP Header Lenght
            *                  NULL if buffer is not big enough
            */


            int rtp_CSRC = -1, rtp_ext = -1;
            int rtp_payload = -1;
            int front_skip = 0, ext_len = 0;

            rtp_payload = buf[1] & 0x7F;
            rtp_CSRC = buf[0] & 0x0F;
            rtp_ext = buf[0] & 0x10;

            if ((P_MPGA == rtp_payload) || (P_MPGV == rtp_payload))
                front_skip = 4;
            else if (P_MPGTS != rtp_payload)
            {
                Console.WriteLine("RTP_process: Unsupported payload type {0}\n", rtp_payload);
                return -1;
            }

            if (rtp_ext == 0) //If there is no RTP Extension then we are confused
            {
                /*
                TRACE( (void)tmfprintf( log, "%s: RTP x-header detected, CSRC=[%d]\n",
                            __func__, rtp_CSRC) );
                */

                if (len < RTP_XTHDRLEN) //if lenght of the packet is less then Extension header then we are even more confused
                {
                    /*
                    TRACE( (void)tmfprintf( log, "%s: RTP x-header requires "
                            "[%lu] bytes, only [%lu] provided\n", __func__,
                            (u_long)(XTLEN_OFFSET + 1), (u_long)len ) );
                    */
                    return null;
                }
            }

            if (rtp_ext != 0) //If there is RTP Header extension than we count it in
            {
                ext_len = XTSIZE + sizeof(int) * ((buf[XTLEN_OFFSET] << 8) + buf[XTLEN_OFFSET + 1]);

            }

            front_skip += RTP_HDR_SIZE + (CSRC_SIZE * rtp_CSRC) + ext_len; //we have to skip that many bytes as front_skip

            return front_skip;
        }

        public byte[] RTP_process()
        {
            /* process RTP package to retrieve the payload
            *
            *
            * @return RTP Payload
            */

            int pkt_len = 0;

            //pkt_len = (int)RTP_hdrlen(); //we need the lenght to cut out and RTP_hdrlen will give that to us
            pkt_len = len; //we need the lenght to cut out and RTP_hdrlen will give that to us

            int rtp_padding = -1;
            int front_skip = 0, back_skip = 0, pad_len = 0;

            front_skip = (int)RTP_hdrlen();


            rtp_padding = buf[0] & 0x20;
            if (rtp_padding != 0) //If there is padding then remove it
            {
                pad_len = buf[pkt_len - 1];
            }

            back_skip += pad_len; //we need to cut that out of the frame (frame has some garbage like CRC at the end that we don't need)

            if (RTP_verify() && (pkt_len < (front_skip + back_skip)))
            {
                Console.WriteLine("RTP_process: invalid header (skip {0} exceeds packet length {1})\n", (long)(front_skip + back_skip), (long)pkt_len);
                RTP_Process_Done = false; //We were unable to process the frame
            }

            pkt_len -= (front_skip + back_skip);
            byte[] RTPPayload = new byte[pkt_len];

            Array.Copy(buf, front_skip, RTPPayload, 0, pkt_len);
            return RTPPayload;
        }
    }
}