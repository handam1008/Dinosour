using UnityEngine;

namespace SSW
{
    public sealed class VsCard : MonoBehaviour
    {
        [SerializeField] JobCatalog _jobs;
        [SerializeField] MenuDinosaurPreview _preview;
        [SerializeField] UnityEngine.UI.Text _name;
        [SerializeField] UnityEngine.UI.Text _job;
        [SerializeField] UnityEngine.UI.Text _tag;

        public RectTransform Rect => (RectTransform)transform;
        public string Name => _name.text;
        public string Job => _job.text;
        public string Tag => _tag.text;

        public void Show(Fighter info, PlayerJob job, bool left, int slot)
        {
            if (!_jobs.TryGet(job, out JobDefinition definition))
                throw new System.ArgumentException("직업 정보를 찾을 수 없습니다.");
            _name.text = info.DisplayName(slot);
            _job.text = definition.KoreanName;
            _tag.text = info.Tag.ToString();
            _tag.gameObject.SetActive(!info.Tag.IsEmpty);
            _preview.Show(definition);
            _preview.SetFacing(left);
        }
    }
}
