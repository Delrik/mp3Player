using DevExpress.Mvvm;
using HtmlAgilityPack;
using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Input;
using System.Windows.Media;

namespace mp3player
{
    struct Data
    {
        private uint _index;
        public uint index
        {
            get
            {
                return _index;
            }
            set
            {
                _index = value;
            }
        }
        private string _name;
        public string name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }
        private string _url;
        public string url
        {
            get
            {
                return _url;
            }
            set
            {
                _url = value;
            }
        }
    }
    class ViewModel : ViewModelBase
    {
        public void check()
        {
            while (true)
            {
                RaisePropertiesChanged("position", "duration", "positionOnDuration");
                Thread.Sleep(500);
            }
        }
        public async void checkPosition()
        {
            await Task.Run(() => check());
        }
        private string _playing = "Currentry playing:\nNothing";
        public string playing
        {
            get
            {
                return _playing;
            }
            set
            {
                _playing = value;
                RaisePropertyChanged();
            }
        }
        bool isChecking = false;
        private BindingList<Data> _gridData = new BindingList<Data>();
        private string _textBoxData = "";
        private Data _selectedItem;
        private MediaPlayer player = new MediaPlayer();
        public string positionOnDuration
        {
            get
            {
                if (player.NaturalDuration.HasTimeSpan)
                {
                    string part1 = Convert.ToInt32(Math.Floor(player.Position.TotalMinutes)).ToString() + ":";
                    string part2 = Convert.ToInt32(Math.Floor(player.Position.TotalSeconds) - Math.Floor(player.Position.TotalMinutes) * 60).ToString();
                    if (part2.Length == 1) part2 = "0" + part2;
                    part2 += "/";
                    string part3 = Convert.ToInt32(Math.Floor(player.NaturalDuration.TimeSpan.TotalMinutes)).ToString() + ":";
                    string part4 = Convert.ToInt32(Math.Floor(player.NaturalDuration.TimeSpan.TotalSeconds) - Math.Floor(player.NaturalDuration.TimeSpan.TotalMinutes) * 60).ToString();
                    if (part4.Length == 1) part4 = "0" + part4;
                    return part1 + part2 + part3 + part4;
                }
                return "0:00/0:00";
            }
        }
        public long duration
        {
            get
            {
                if(player.NaturalDuration.HasTimeSpan) return player.NaturalDuration.TimeSpan.Ticks;
                return 100;
            }
        }
        public long position
        {
            get
            {
                return player.Position.Ticks;
            }
            set
            {
                player.Position = new TimeSpan(value);
                RaisePropertyChanged();
            }
        }
        public Data selectedItem
        {
            get
            {
                return _selectedItem;
            }
            set
            {
                _selectedItem = value;
                RaisePropertyChanged();
            }
        }
        public string textBoxData
        {
            get
            {
                return _textBoxData;
            }
            set
            {
                _textBoxData = value;
                RaisePropertyChanged();
            }
        }
        public BindingList<Data> gridData
        {
            get
            {
                return _gridData;
            }
            set
            {
                gridData = value;
                RaisePropertyChanged(() => _gridData);
            }
        }
        public ICommand playBtn
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    if (player.Source != null) player.Play();
                });
            }
        }
        public ICommand stopBtn
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    player.Stop();
                });
            }
        }
        public ICommand pauseBtn
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    if(player.CanPause) player.Pause();
                });
            }
        }
        public ICommand playTrackBtn
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    if (selectedItem.url == "") return;
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://z1.fm");
                    request.Credentials = CredentialCache.DefaultCredentials;
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    string cookie = response.Headers.Get("set-cookie");
                    request = (HttpWebRequest)WebRequest.Create("https://z1.fm" + selectedItem.url);
                    request.Headers.Add(HttpRequestHeader.Cookie, cookie);
                    request.Method = "GET";
                    request.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/85.0.4183.121 Safari/537.36");
                    response = (HttpWebResponse)request.GetResponse();
                    Uri uri = response.ResponseUri;
                    string mp3 = uri.Scheme + "://" + uri.Host + uri.AbsolutePath;

                    player.Open(new Uri(mp3));
                    player.Play();
                    RaisePropertiesChanged("duration", "position", "positionOnDuration");
                    if (!isChecking)
                    {
                        isChecking = true;
                        checkPosition();
                    }
                    playing = "Currently playing\n" + selectedItem.name;
                });
            }
        }
        public ICommand btnClick
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    _gridData.Clear();
                    Data trackBuf;
                    if (textBoxData.Length == 0)
                    {
                        trackBuf = new Data();
                        trackBuf.index = 0;
                        trackBuf.name = "Wrong request";
                        trackBuf.url = "";
                        _gridData.Add(trackBuf);
                        return;
                    }
                    //
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://z1.fm");
                    request.Credentials = CredentialCache.DefaultCredentials;
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    string cookie = response.Headers.Get("set-cookie");
                    //
                    request = (HttpWebRequest)WebRequest.Create("https://z1.fm/mp3/search?keywords=" + textBoxData);
                    request.Headers.Add(HttpRequestHeader.Cookie, cookie);
                    response = (HttpWebResponse)request.GetResponse();
                    StreamReader sr = new StreamReader(response.GetResponseStream());
                    string str = sr.ReadToEnd();
                    HtmlDocument document = new HtmlDocument();
                    document.LoadHtml(str);
                    HtmlNode body = document.GetElementbyId("container");
                    HtmlNodeCollection tracks = body.SelectNodes(body.XPath + "//div[contains(@class, 'song song-xl')]");
                    if (tracks == null)
                    {
                        trackBuf = new Data();
                        trackBuf.index = 0;
                        trackBuf.name = "Wrong request";
                        trackBuf.url = "";
                        _gridData.Add(trackBuf);
                        return;
                    }
                    string name;
                    for(int i = 0; i<tracks.Count;i++)
                    {
                        trackBuf = new Data();
                        name = tracks[i].SelectSingleNode(tracks[i].XPath + "//div[contains(@class,'song-artist')]").LastChild.LastChild.InnerText;
                        name = name + " ‒ " + tracks[i].SelectSingleNode(tracks[i].XPath + "//div[contains(@class, 'song-name')]").LastChild.LastChild.InnerText;
                        trackBuf.url = tracks[i].SelectSingleNode(tracks[i].XPath + "//span[contains(@class, 'song-play btn4 play')]").GetAttributeValue("data-url", "Error!");
                        trackBuf.name = HttpUtility.HtmlDecode(name);
                        trackBuf.index = (uint)_gridData.Count + 1;
                        _gridData.Add(trackBuf);
                    }
                });
            }
        }
    }
}
