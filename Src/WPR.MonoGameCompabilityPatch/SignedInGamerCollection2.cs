using Microsoft.Xna.Framework;
//using Microsoft.Xna.Framework.GamerServices;

namespace Microsoft.Xna.Framework.GamerServices
{

    public class SignedInGamerCollection2 //: GamerCollection<SignedInGamer>
    {
        private SignedInGamerCollection2 _SignedInGamers;
        public  SignedInGamerCollection2 SignedInGamers
        { 
          get
            {
                return default;
            }
          set
            {
                _SignedInGamers = value;
            }
         }

        //public static SignedInGamerCollection2  get_SignedInGamers()
        //{
        //    return default;
        //}

        /*public SignedInGamerCollection2(List<SignedInGamer> gamerList)
            : base(gamerList)
        {

        }*/

        public SignedInGamer this[Microsoft.Xna.Framework.GamerServices.PlayerIndex index]
        {
            get
            {
                for (int i = 0; i < /*base.Count*/1; i++)
                {
                    SignedInGamer gamer = default;//base[i];
                    if (gamer.PlayerIndex == index)
                    {
                        return gamer;
                    }
                }
                return null;
            }
        }
    }

}
