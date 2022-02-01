namespace GameServer
{
    class WebcamTexture
    {
        public int id;
        public string username;
        public byte[] texture;

        public WebcamTexture(int mId, string mUsername, byte[] mTexture)
        {
            id = mId;
            username = mUsername;
            texture = mTexture;
        }
    }
}
