namespace RenderingLibrary.Content
{
    public class LoaderManager
    {
        #region Fields

        static LoaderManager mSelf;


        #endregion

        #region Properties

        public static LoaderManager Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new LoaderManager();
                }
                return mSelf;
            }
        }

        public IContentLoader ContentLoader
        {
            get;
            set;
        }


        #endregion

    }
}
