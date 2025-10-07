namespace Wlniao.Aliyun
{
    /// <summary>
    /// 阿里云密钥
    /// </summary>
    public class AccessKey
    {
        private static string _keyId = null;
        private static string _keySecret = null;
        /// <summary>
        /// 【WLN_ALIYUN_KEYID】Access Key Id
        /// </summary>
        public static string KeyId
        {
            get
            {
                _keyId ??= Config.GetSetting("WLN_ALIYUN_KEYID");
                return _keyId;
            }
            set => _keyId = value;
        }
        /// <summary>
        /// 【WLN_ALIYUN_KEYSECRET】Access Key Secret
        /// </summary>
        public static string KeySecret
        {
            get
            {
                _keySecret ??= Config.GetSetting("WLN_ALIYUN_KEYSECRET");
                return _keySecret;
            }
            set => _keySecret = value;
        }
    }
}