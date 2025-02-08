using UnityEngine;

namespace LeaderbordManager
{


    public class ScoreRowElement : MonoBehaviour
    {
        [SerializeField] protected TextWriter usernameText;
        [SerializeField] protected TextWriter scoreText;
        [SerializeField] protected TextWriter dateText;
        [SerializeField] protected ScoreType scoreType; // Add this field

        public virtual void Set(string username, float score, string date)
        {
            usernameText.Set(username);
            scoreText.Set(score, scoreType == ScoreType.Integer ? "" : "F2");
            dateText.Set(date);
        }

        public virtual void SetWithMine(string username, float score, string date)
        {
            Set(username, score, date);
            usernameText.Set(Color.red);
            scoreText.Set(Color.red);
            dateText.Set(Color.red);
        }

    }
}
