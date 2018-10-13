using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace multicast2unicast
{
    public class rtp
    {

        //buffer with data
        public byte[] buf; //data to process
        static int len;
        public int RTPOK = -1;
        byte[] pbuf; //only RTP Payload
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
        const byte P_MPGTS = 0x21;     /* MPEG TS    */

        enum RTPPayloadTypes : int
        {
            /* MPEG payload-type constants - adopted from VLC 0.8.6 */
            P_MPGA = 0x0E,     /* MPEG audio */
            P_MPGV = 0x20,     /* MPEG video */
            P_MPGTS = 0x21,     /* MPEG TS    */
        }


        int is_rtp = 0;

        public int RTP_check()
        {
            int error = 9;
            rtp _rtp = new rtp();
            /* check if the buffer is an RTP packet
            *
            * @param buf       buffer to analyze
            * @len             size of the buffer
            * @param is_rtp    variable to set to 1 if data is an RTP packet
            * @param log       log file
             *
            * @return 0 if there was no error, -1 otherwise
            */

            int rtp_version = 0;
            //int rtp_payload_type = 0;
            RTPPayloadTypes rtp_payload_type;

            if (len < RTP_MIN_SIZE)
            {
                Console.WriteLine("RTP_check: buffer size {0} is less than minimum {1}\n", (long)len, (long)RTP_MIN_SIZE);
                error = -1;
            }
            /* initial assumption: is NOT RTP */
            _rtp.is_rtp = 0;

            if (MPEG_TS_SIG == buf[0])
            {
                //Console.WriteLine("MPEG-TS stream detected\n");
                error = 0;
            }

            rtp_version = (buf[0] & 0xC0) >> 6;
            Debug.WriteLine("RTP Version: " + rtp_version);
            if (RTP_VER2 != rtp_version)
            {
                Console.WriteLine("RTP_check: wrong RTP version {0} must be {1}\n", (int)rtp_version, (int)RTP_VER2);
                error = -1;

            }

            if (len < RTP_HDR_SIZE)
            {
                Console.WriteLine("RTP_check: header size is too small {0}\n", (long)len);
                error = -1;
            }

            _rtp.is_rtp = 1;

            rtp_payload_type = (RTPPayloadTypes)(buf[1] & 0x7F);
            Debug.WriteLine("RTP Payload Type: " + rtp_payload_type);
            switch (rtp_payload_type)
            {
                case RTPPayloadTypes.P_MPGA:
                    Console.WriteLine("RTP_check: {0} MPEG audio stream\n", (int)rtp_payload_type);
                    break;
                case RTPPayloadTypes.P_MPGV:
                    Console.WriteLine("RTP_check: {0} MPEG video stream\n", (int)rtp_payload_type);
                    break;
                case RTPPayloadTypes.P_MPGTS:
                    Console.WriteLine("RTP_check: {0} MPEG TS stream\n", (int)rtp_payload_type);
                    break;

                default:
                    Console.WriteLine("RTP_check: unsupported RTP payload type {0}\n", (int)rtp_payload_type);
                    return -1;
            }
            return error;
        }
        public int RTP_verify()
        {
            /* verify if buffer contains an RTP packet, 0 otherwise
            *
            * @param buf       buffer to analyze
            * @param len       size of the buffer
            * @param log       error log
            *
            * @return 1 if buffer contains an RTP packet, 0 otherwise
            */
            int rtp_version = 0;
            int rtp_payload_type = 0;

            if ((len < RTP_MIN_SIZE) || (len < RTP_HDR_SIZE))
            {
                Console.WriteLine("RTP_verify: inappropriate size={0} of RTP packet\n", (long)len);
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
            }

            switch (rtp_payload_type)
            {
                case P_MPGA:
                case P_MPGV:
                case P_MPGTS:
                    break;
                default:
                    Console.WriteLine("RTP_verify: unsupported RTP payload type {0}\n", (int)rtp_payload_type);
                    return 0;
            }

            return 1;
        }
        int RTP_hdrlen(byte[] buffer, int bufferSize, int hdrlen)
        {
            /* calculate length of an RTP header
            *
            * @param buf       buffer to analyze
            * @param len       size of the buffer
            * @param hdrlen    pointer to header length variable
            * @param log       error log
            *
            * @return          0 if header length has been calculated,
            *                  ENOMEM(9) if buffer is not big enough
            */
            int rtp_CSRC = -1, rtp_ext = -1;
            int rtp_payload = -1;
            int front_skip = 0,
                    ext_len = 0;

            //assert(buf && (len >= RTP_HDR_SIZE) && hdrlen && log);


            rtp_payload = buffer[1] & 0x7F;
            rtp_CSRC = buffer[0] & 0x0F;
            rtp_ext = buffer[0] & 0x10;

            /* profile-based skip: adopted from vlc 0.8.6 code */
            if ((P_MPGA == rtp_payload) || (P_MPGV == rtp_payload))
                front_skip = 4;
            else if (P_MPGTS != rtp_payload)
            {
                Console.WriteLine("RTP_process: Unsupported payload type {0}\n", rtp_payload);
                return -1;
            }

            if (rtp_ext == -1)
            {
                /*
                TRACE( (void)tmfprintf( log, "%s: RTP x-header detected, CSRC=[%d]\n",
                            __func__, rtp_CSRC) );
                */

                if (len < RTP_XTHDRLEN)
                {
                    /*
                    TRACE( (void)tmfprintf( log, "%s: RTP x-header requires "
                            "[%lu] bytes, only [%lu] provided\n", __func__,
                            (u_long)(XTLEN_OFFSET + 1), (u_long)len ) );
                    */
                    return 9;
                }
            }

            if (rtp_ext != -1)
            {
                ext_len = XTSIZE +
                    sizeof(int) * ((buf[XTLEN_OFFSET] << 8) + buf[XTLEN_OFFSET + 1]);
            }

            front_skip += RTP_HDR_SIZE + (CSRC_SIZE * rtp_CSRC) + ext_len;

            hdrlen = front_skip;
            return 0;
        }
        public byte[] RTP_process(int verify)
        {
            /* process RTP package to retrieve the payload: set
            * pbuf to the start of the payload area; set len to
            * be equal payload's length
            *
            * @param pbuf      address of pointer to beginning of RTP packet
            * @param len       pointer to RTP packet's length
            * @param verify    verify that it is an RTP packet if != 0
            * @param log       log file
            *
            * @return 0 if there was no error, -1 otherwise;
            *         set pbuf to point to beginning of payload and len
            *         be payload size in bytes
            */
            int rtp_padding = -1;
            int front_skip = 0, back_skip = 0, pad_len = 0;

            int pkt_len = 0;

            //assert(pbuf && len && log);
            //buf = *pbuf;
            pbuf = buf;
            //pkt_len = *len;
            len = pkt_len;

            /*
            if (verify != 1 && RTP_verify() != 1)
                RTPOK = - 1;

            if (0 != RTP_hdrlen(buf, pkt_len, front_skip)) //?????
                RTPOK = - 1;

            */

            rtp_padding = buf[0] & 0x20;
            if (rtp_padding != -1) //???????
            {
                pad_len = buf[pkt_len - 1];
            }

            back_skip += pad_len;

            if ((verify != -1) && (pkt_len < (front_skip + back_skip))) //???????
            {
                Console.WriteLine("RTP_process: invalid header (skip {0} exceeds packet length {1})\n", (long)(front_skip + back_skip), (long)pkt_len);
                RTPOK = - 1;
            }

            /* adjust buffer/length to skip heading and padding */
            /*
            TRACE( (void)tmfprintf( log, "In: RTP buf=[%p] of [%lu] bytes, "
                        "fskip=[%ld], bskip=[%lu]\n",
                        (void*)buf, (u_long)pkt_len,
                        (u_long)front_skip, (u_long)back_skip ) );
            */

            //buf += front_skip;
            //buf[buf] = front_skip;
            //pkt_len -= (front_skip + back_skip);

            /*
            TRACE( (void)tmfprintf( log, "Out RTP buf=[%p] of [%lu] bytes\n",
                        (void*)buf, (u_long)pkt_len ) );
            */
            pbuf = buf;
            len = pkt_len;

            RTPOK = 0;
            return pbuf;
        }
    }
}
