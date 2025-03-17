namespace GFDeckMaid.Enums;
public enum Piles
    {
        Discard,
        Facedown,
        Faceup,
        lostSouls
    }

    public static class PilesExtensions
    {
        public static bool IsPublic(this Piles piles)
        {
            switch (piles)
            {
                case Piles.Discard:
                case Piles.Faceup:
                case Piles.lostSouls:
                return true;
                default:
                    return false;
            }
        }
    }

