namespace Microsoft.Xna.Framework.GamerServices
{
    public class PlayerIndex
    {
        public static PlayerIndex One = new PlayerIndex(0);
        private int v;

        public PlayerIndex(int v)
        {
            this.v = v;
        }
    }
}