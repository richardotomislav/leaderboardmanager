using UnityEngine;

namespace LeaderbordManager
{
    public class ExtraRowElement : ScoreRowElement
    {
        [SerializeField] private TextWriter extraText;

        public void Set(string username, int score, string date, string extra)
        {
            base.Set(username, score, date);
            extraText.Set(extra);
        }

        public void SetWithMine(string username, float score, string date, string extra)
        {
            base.SetWithMine(username, score, date);
            extraText.Set(extra);
            extraText.Set(Color.red);
        }
    }
}
