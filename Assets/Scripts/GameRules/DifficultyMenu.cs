using UnityEngine;
using QuestBilliards.Inputs;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

namespace QuestBilliards.GameRules
{
    [DefaultExecutionOrder(-300)]
    public sealed class DifficultyMenu : MonoBehaviour
    {
        public BilliardsPlayerMode playerMode;
        public BilliardsInputReader inputs;
        public Transform head;
        public PocketDifficultyController table;
        public Material panelMaterial;
        public bool IsChoosing { get; private set; } = true;
        public int SelectedIndex { get; private set; }
        private GameObject panel;
        private readonly Renderer[] buttons = new Renderer[3];
        private readonly Collider[] hitboxes = new Collider[3];
        private bool previousTrigger;
        private bool navigationHeld;
        private GameObject cueStatus;
        private readonly string[] labels = { "FACIL  -  troneras de 20 cm", "NORMAL  -  troneras de 16 cm", "DIFICIL  -  troneras de 13 cm" };
        private void Awake()
        {
            Application.runInBackground = true;
            if (playerMode != null) playerMode.ControlsLocked = true;
            var grab = FindFirstObjectByType<QuestBilliards.CueInteraction.CueGrabController>();
            if (grab != null && grab.statusLabel != null) { cueStatus = grab.statusLabel.gameObject; cueStatus.SetActive(false); }
        }
        private void Start()
        {
            if (playerMode != null) playerMode.ControlsLocked = true;
            panel = new GameObject("DifficultySelection"); panel.transform.SetParent(head, false); panel.transform.localPosition = new Vector3(0,0,1.1f);
            Box("Background", new Vector3(0,0,0.035f), new Vector3(0.85f,0.77f,0.015f), new Color(0.025f,0.04f,0.07f), false);
            Text("BILLAR VR", new Vector3(0,0.29f,-0.005f), 0.018f, Color.white);
            Text("Elige la dificultad", new Vector3(0,0.20f,-0.005f), 0.012f, Color.white);
            for (int i=0;i<3;i++)
            {
                float y=0.07f-i*0.135f;
                var go=Box("Difficulty_"+i,new Vector3(0,y,0),new Vector3(0.74f,0.105f,0.012f),Color.gray,true);
                buttons[i]=go.GetComponent<Renderer>(); hitboxes[i]=go.GetComponent<Collider>();
                Text(labels[i],new Vector3(0,y,-0.012f),0.011f,Color.white);
            }
            Text("Joystick arriba/abajo: elegir | Gatillo: jugar\nSIM: Y/H y T | UNITY: 1, 2 o 3",new Vector3(0,-0.325f,-0.005f),0.007f,Color.white);
            Highlight();
        }
        private GameObject Box(string name,Vector3 pos,Vector3 scale,Color color,bool collider)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(panel.transform,false);go.transform.localPosition=pos;go.transform.localScale=scale;
            var mat=new Material(panelMaterial);mat.color=color;go.GetComponent<Renderer>().sharedMaterial=mat;
            if(collider)go.GetComponent<Collider>().isTrigger=true;else Destroy(go.GetComponent<Collider>());
            return go;
        }
        private void Text(string value,Vector3 pos,float size,Color color)
        {
            var go=new GameObject(value);go.transform.SetParent(panel.transform,false);go.transform.localPosition=pos;
            var text=go.AddComponent<TextMesh>();text.text=value;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.characterSize=size;text.fontSize=48;text.color=color;
        }
        private void Highlight()
        {
            for(int i=0;i<3;i++)if(buttons[i]!=null)buttons[i].sharedMaterial.color=i==SelectedIndex?new Color(0.02f,0.42f,0.23f):new Color(0.09f,0.14f,0.23f);
        }
        public void Choose(BilliardsDifficulty level)
        {
            if(!IsChoosing)return;
            table.Apply(level);SelectedIndex=(int)level;IsChoosing=false;
            if(panel!=null)panel.SetActive(false);
            playerMode.ControlsLocked=false;
            if(cueStatus!=null)cueStatus.SetActive(true);
        }
        private void Update()
        {
            if(!IsChoosing)return;
            if (playerMode != null) playerMode.ControlsLocked = true;
            if(cueStatus!=null)cueStatus.SetActive(false);
            bool trigger=inputs!=null&&(inputs.Right.Trigger||inputs.Left.Trigger);
            float axis=inputs!=null?inputs.Right.Stick.y+inputs.Left.Stick.y:0;
            if(Mathf.Abs(axis)>0.5f&&!navigationHeld){SelectedIndex=(SelectedIndex+(axis>0?-1:1)+3)%3;Highlight();}
            navigationHeld=Mathf.Abs(axis)>0.25f;
#if UNITY_EDITOR
            var keys=Keyboard.current;
            if(keys!=null)
            {
                if(keys.digit1Key.wasPressedThisFrame){Choose(BilliardsDifficulty.Easy);return;}
                if(keys.digit2Key.wasPressedThisFrame){Choose(BilliardsDifficulty.Normal);return;}
                if(keys.digit3Key.wasPressedThisFrame){Choose(BilliardsDifficulty.Hard);return;}
                if(keys.enterKey.wasPressedThisFrame){Choose((BilliardsDifficulty)SelectedIndex);return;}
            }
            if(Mouse.current!=null&&Mouse.current.leftButton.wasPressedThisFrame&&Camera.main!=null)
            {
                Ray ray=Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
                for(int i=0;i<3;i++)if(hitboxes[i]!=null&&hitboxes[i].Raycast(ray,out _,8f)){Choose((BilliardsDifficulty)i);return;}
            }
#endif
            if(trigger&&!previousTrigger)Choose((BilliardsDifficulty)SelectedIndex);
            previousTrigger=trigger;
        }
        private void OnDestroy()
        {
            if(playerMode!=null)playerMode.ControlsLocked=false;
            if(panel!=null){foreach(var r in panel.GetComponentsInChildren<Renderer>(true))if(r is MeshRenderer&&r.gameObject.name!="BILLAR VR"&&r.GetComponent<TextMesh>()==null)Destroy(r.sharedMaterial);Destroy(panel);}
        }
    }
}
