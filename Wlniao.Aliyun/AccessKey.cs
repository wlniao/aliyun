namespace Wlniao.Aliyun
{
    /// <summary>
    /// 阿里云密钥
    /// </summary>
    public class AccessKey
    {
        private static string _KeyId = null;
        private static string _KeySecret = null;
        /// <summary>
        /// 【WLN_ALIYUN_KEYID】Access Key Id
        /// </summary>
        public static string KeyId
        {
            get
            {
                if (_KeyId == null)
                {
                    _KeyId = Config.GetSetting("WLN_ALIYUN_KEYID");
                }
                return _KeyId;
            }
            set
            {
                _KeyId = value;
            }
        }
        /// <summary>
        /// 【WLN_ALIYUN_KEYSECRET】Access Key Secret
        /// </summary>
        public static string KeySecret
        {
            get
            {
                if (_KeySecret == null)
                {
                    _KeySecret = Config.GetSetting("WLN_ALIYUN_KEYSECRET");
                }
                return _KeySecret;
            }
            set
            {
                _KeySecret = value;
            }
        }
    }
}