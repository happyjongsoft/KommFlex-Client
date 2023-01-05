using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Communicate with server
namespace KommFlex
{
    class CommController
    {
        // Server URL
        string m_serverBaseUrl = "";
        string m_stationId = "";
        static Random rnd = new Random();

        public void setServerBaseUrl(string addr)
        {
            m_serverBaseUrl = addr;
        }
        
        public void setStationId(string station_id){
            m_stationId = station_id;
        }
        public JObject postData(string api_name, System.Collections.Specialized.NameValueCollection param)
        {
            // Post some text data to server
            try
            {
                using (WebClient client = new WebClient())
                {
                    byte[] responsebytes = client.UploadValues(m_serverBaseUrl + "/"+ api_name, "POST", param);
                    string responsebody = Encoding.UTF8.GetString(responsebytes);

                    JObject resobj = JObject.Parse(responsebody);
                    return resobj;
                }
            }
            catch (Exception e)
            {
                JObject resobj = JObject.Parse("");
                return resobj;
            }
        }

        public int updateChannelInfo(string rtc_id, string endpoint_id, string endpoint_type, string station_id)
        {
            // API
            // Update my WebRTC ID whenever logged in chat room
            try
            {
                // Logged in Happy Video Chat room
                var param = new System.Collections.Specialized.NameValueCollection();
                param.Add("RTCId", rtc_id);
                param.Add("EndpointId", endpoint_id);
                param.Add("EndpointType", endpoint_type);
                param.Add("StationId", station_id);
                JObject resobj = postData("UpdateChannelInfo", param);
                Console.WriteLine(param.ToString());
                Console.WriteLine(resobj.ToString());
                if (Int32.Parse(resobj["return_code"].ToString()) != 1000)
                {
                    return -1;
                }
                else
                {
                    return 1;
                }

            }
            catch
            {
                return -1;
            }
        }

        public int getEndpointTypeById(string id)
        {
            // API
            // Get EndpointType by EndpointId
            try
            {
                var param = new System.Collections.Specialized.NameValueCollection();
                param.Add("EndpointId", id);
                Console.WriteLine(param.ToString());

                JObject resobj = postData("GetEndpointById", param);

                if (Int32.Parse(resobj["return_code"].ToString()) != 1000)
                {
                    return -1;
                }
                else
                {
                    return Int32.Parse(resobj["data"]["Type"].ToString());
                }
            }
            catch
            {
                return -1;
            }

        }

        public int getDispatcherIdByClientId(string id)
        {
            // API
            // Get EndpointType by EndpointId
            try
            {
                var param = new System.Collections.Specialized.NameValueCollection();
                param.Add("EndpointId", id);
                Console.WriteLine(param.ToString());

                JObject resobj = postData("GetEndpointById", param);

                if (Int32.Parse(resobj["return_code"].ToString()) != 1000)
                {
                    return -1;
                }
                else
                {
                    return Int32.Parse(resobj["data"]["DispatcherId"].ToString());
                }
            }
            catch
            {
                return -1;
            }

        }

        public int getEndpointIdByRTCId(string rtcid)
        {
            // API
            // Get EndpointId by RTCId
            try
            {
                var param = new System.Collections.Specialized.NameValueCollection();
                param.Add("RTCId", rtcid);
                Console.WriteLine(param.ToString());

                JObject resobj = postData("GetEndpointIdByRTCId", param);
                Console.WriteLine(resobj.ToString());
                if (Int32.Parse(resobj["return_code"].ToString()) != 1000)
                {
                    return -1;
                }
                else
                {
                    return Int32.Parse(resobj["data"]["EndpointId"].ToString());
                }
            }
            catch
            {
                return -1;
            }

        }

        public string getEndpointNameById(int id)
        {
            // API
            // return EndpointName allocated Id
            string endpointName = "";
            if (id == -1)
                return endpointName;

            try
            {
                var param = new System.Collections.Specialized.NameValueCollection();
                param.Add("EndpointId", id.ToString());
                Console.WriteLine(param.ToString());

                JObject resobj = postData("GetEndpointById", param);
                Console.WriteLine(resobj.ToString());
                if (Int32.Parse(resobj["return_code"].ToString()) == 1000)
                {
                    endpointName = resobj["data"]["Name"].ToString().Trim();
                }
            }
            catch
            {}

            return endpointName;

        }
        public string getRTCIdByEndpointId(int id)
        {
            // API
            // return EndpointName allocated Id
            string rtcId = "";
            if (id == -1)
                return rtcId;

            try
            {
                var param = new System.Collections.Specialized.NameValueCollection();
                param.Add("EndpointId", id.ToString());
                Console.WriteLine(param.ToString());

                JObject resobj = postData("GetRTCIdByEndpointId", param);
                Console.WriteLine(resobj.ToString());
                if (Int32.Parse(resobj["return_code"].ToString()) == 1000)
                {
                    rtcId = resobj["data"]["RTCId"].ToString().Trim();
                }
            }
            catch
            { }

            return rtcId;

        }

        public string getEndpointNameByRTCId(string rtcid)
        {
            // API
            // return EndpointName allocated RTCId

            if (rtcid.Trim().Length == 0)
                return "";

            string endpointName = "";

            int endpointId = getEndpointIdByRTCId(rtcid);

            Console.WriteLine("EndpointId : " + endpointId.ToString());
            endpointName = getEndpointNameById(endpointId);

            return endpointName.Trim();
        }

        public List<JObject> getDispatchers()
        {
            // API
            // Get connected all dispatcher list
            try
            {
                var param = new System.Collections.Specialized.NameValueCollection();
                param.Add("StationId", m_stationId);
                JObject resobj = postData("GetDispatchers", param);

                Console.WriteLine(resobj.ToString());
                if (Int32.Parse(resobj["return_code"].ToString()) != 1000)
                {
                    return new List<JObject>();
                }
                else
                {
                    return resobj["data"].ToObject<List<JObject>>();
                }
            }
            catch {
                return new List<JObject>();
            }
        }

        public string findAvailableDispatcherRTCId(List<string> rtc_id_list)
        {
            // API
            // Find a dispatcher to communicate
            try
            {
                List<JObject> all_dispatchers = getDispatchers();

                List<string> live_rtc_id_list = new List<string>();

                foreach (JObject dispatcher in all_dispatchers)
                {
                    string dispatcher_rtc_id = dispatcher["RTCId"].ToString().Trim();

                    Console.WriteLine(dispatcher_rtc_id);
                    foreach (string rtc_id in rtc_id_list)
                    {
                        Console.WriteLine(">>" + rtc_id);
                        if (dispatcher_rtc_id.Equals(rtc_id))
                        {
                            live_rtc_id_list.Add(rtc_id);

                            Console.Write(">>>>>>>>>>>>>" + rtc_id);
                        }
                    }
                }

                if (live_rtc_id_list.Count == 0)
                    return "";

                int r = rnd.Next(live_rtc_id_list.Count);
                return (string)live_rtc_id_list[r];

            }
            catch
            {
                return "";
            }
        }

        public string findCentralDispatcherRTCId(List<string> rtc_id_list)
        {
            // API
            // Find Central Dispatcher when try to redirect
            try
            {
                List<JObject> all_dispatchers = getDispatchers();

                List<string> live_rtc_id_list = new List<string>();

                foreach (JObject dispatcher in all_dispatchers)
                {
                    string dispatcher_rtc_id = dispatcher["RTCId"].ToString().Trim();
                    int endpoint_type = Int32.Parse(dispatcher["Type"].ToString().Trim());

                    if (endpoint_type == 0) // Central Dispatcher
                    {
                        foreach (string rtc_id in rtc_id_list)
                        {
                            if (dispatcher_rtc_id.Equals(rtc_id))
                            {
                                live_rtc_id_list.Add(rtc_id);
                            }
                        }
                    }
                }

                if (live_rtc_id_list.Count == 0)
                    return "";

                int r = rnd.Next(live_rtc_id_list.Count);
                return (string)live_rtc_id_list[r];

            }
            catch
            {
                return "";
            }
        }

        public string getServerTime()
        {
            // API
            // return EndpointName allocated Id
            string cur_time = "";

            try
            {
                var param = new System.Collections.Specialized.NameValueCollection();

                JObject resobj = postData("GetServerTime", param);
                Console.WriteLine(resobj.ToString());
                if (Int32.Parse(resobj["return_code"].ToString()) == 1000)
                {
                    cur_time = resobj["data"].ToString().Trim();
                }
            }
            catch
            { }

            return cur_time;
        }

        public int AddCallLog(string client_endpoint_id, string dispatcher_endpoint_id, string start_time, string end_time, int passed_to_center)
        {
            // API
            // Add Call log in server
            try
            {
                Console.WriteLine("==== CALL LOG =====");

                var param = new System.Collections.Specialized.NameValueCollection();
                param.Add("ClientEndpointId", client_endpoint_id);
                param.Add("DispatcherEndpointId", dispatcher_endpoint_id);
                param.Add("StartTime", start_time);
                param.Add("EndTime", end_time);
                param.Add("PassedToCenter", passed_to_center.ToString());

                JObject resobj = postData("AddCallLog", param);

                Console.WriteLine(resobj.ToString());
                if (Int32.Parse(resobj["return_code"].ToString()) == 1000)
                {
                    return Int32.Parse(resobj["data"]["id"].ToString());
                }
                else
                {
                    return -1;
                }
            }
            catch
            {
                return -1;
            }
        }

        public int AddPictureLog(string logid, List<string> picturelist)
        {
            if(Int32.Parse(logid) < 0)
                return -1;
            if (picturelist.Count <= 0)
                return 1;

            try
            {
                Console.WriteLine("==== PICTURE LOG =====");

                var param = new System.Collections.Specialized.NameValueCollection();
                param.Add("LogId", logid);

                JArray obj_picturelist = new JArray();
                foreach(string pname in picturelist)
                {
                    obj_picturelist.Add(
                        pname
                        );
                }
                param.Add("PictureList", obj_picturelist.ToString());
                
                JObject resobj = postData("AddPictureLog", param);

                Console.WriteLine(resobj.ToString());
                if (Int32.Parse(resobj["return_code"].ToString()) != 1000)
                {
                    return 1;
                }
                else
                {
                    return -1;
                }
            }
            catch
            {
                return -1;
            }
        }
        public void sendPing(string endpoint_id)
        {
            // ping to server
            try
            {
                var param = new System.Collections.Specialized.NameValueCollection();
                param.Add("EndpointId", endpoint_id);
                JObject resobj = postData("PingFromEndpoint", param);
                Console.WriteLine(resobj.ToString());
            }
            catch
            {
                Console.WriteLine("PING FAILED");
            }
        }

    }

}
