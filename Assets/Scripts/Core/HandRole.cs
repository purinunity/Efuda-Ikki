namespace EfudaIkki.Core
{
    // Integer values mirror the existing global HandEvaluator.HandRank API.
    public enum HandRole
    {
        Miezu = 0,
        Isso = 1,
        Niso = 2,
        Sanju = 3,
        Hikari = 4,
        Suzi = 5,
        Yonju = 6,
        Tenshu = 7,
        Nanahikari = 8,
        Nanasuzi = 9,
        Tenshukaku = 10
    }

    public static class HandRoleRules
    {
        public static int GetScore(HandRole role)
        {
            switch (role)
            {
                case HandRole.Isso: return 5;
                case HandRole.Niso: return 10;
                case HandRole.Sanju: return 20;
                case HandRole.Hikari: return 30;
                case HandRole.Suzi: return 35;
                case HandRole.Yonju: return 40;
                case HandRole.Tenshu: return 45;
                case HandRole.Nanahikari: return 60;
                case HandRole.Nanasuzi: return 70;
                case HandRole.Tenshukaku: return 90;
                default: return 0;
            }
        }

        public static string GetDisplayName(HandRole role)
        {
            switch (role)
            {
                case HandRole.Isso: return "一双";
                case HandRole.Niso: return "二双";
                case HandRole.Sanju: return "三珠";
                case HandRole.Hikari: return "光";
                case HandRole.Suzi: return "筋";
                case HandRole.Yonju: return "四珠";
                case HandRole.Tenshu: return "天守";
                case HandRole.Nanahikari: return "七光";
                case HandRole.Nanasuzi: return "七筋";
                case HandRole.Tenshukaku: return "天守閣";
                default: return "不見";
            }
        }

        public static bool IsHigher(HandRole candidate, HandRole current)
        {
            int candidateScore = GetScore(candidate);
            int currentScore = GetScore(current);
            return candidateScore != currentScore
                ? candidateScore > currentScore
                : (int)candidate > (int)current;
        }
    }
}
