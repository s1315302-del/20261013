using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using HarborRhythm;

public static class HarborSceneBuilder
{
    const string ScenePath = "Assets/Scenes/KaohsiungHarbor.unity";
    static Transform root;
    static Material navy, concrete, brick, roof, cyan, gold, pink, water, glass, steel, foliage, window, stone;

    [MenuItem("Harbor Rhythm/Build Kaohsiung 3D Scene")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before building.");
        var previous = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (previous.IsValid() && string.IsNullOrEmpty(previous.path))
        {
            Directory.CreateDirectory("Assets/Scenes");
            string backup=AssetDatabase.GenerateUniqueAssetPath("Assets/Scenes/BeforeHarborDetailUpdate.unity");
            if (!EditorSceneManager.SaveScene(previous,backup)) throw new IOException("Unable to preserve the current untitled scene.");
        }
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
        Directory.CreateDirectory("Assets/HarborRhythm/Materials");
        navy = Mat("Midnight metal", new Color(.045f,.09f,.15f));
        concrete = Mat("Warm limestone", new Color(.68f,.62f,.52f));
        brick = Mat("Warehouse brick", new Color(.48f,.20f,.15f));
        roof = Mat("Warehouse roof", new Color(.11f,.19f,.24f));
        cyan = Mat("Lagoon neon", new Color(.12f,.94f,.82f), 2);
        gold = Mat("Sunset gold", new Color(1,.62f,.18f), 2);
        pink = Mat("Festival coral", new Color(1,.25f,.38f), 2);
        water = Mat("Harbor water", new Color(.045f,.27f,.34f));
        glass = Mat("Music center panels", new Color(.17f,.36f,.43f));
        steel = Mat("Brushed aluminum",new Color(.58f,.63f,.65f)); steel.SetFloat("_Metallic",.72f); steel.SetFloat("_Smoothness",.3f);
        foliage = Mat("Harbor greenery",new Color(.18f,.28f,.16f)); foliage.SetFloat("_Smoothness",.05f);
        window = Mat("Warm interior windows",new Color(.95f,.74f,.42f),.35f);
        stone = Mat("Distant city concrete",new Color(.36f,.42f,.46f)); stone.SetFloat("_Smoothness",.12f);
        brick.SetTexture("_BaseMap",SurfaceTexture("Weathered brick",true)); brick.SetTextureScale("_BaseMap",new Vector2(7,3)); brick.SetFloat("_Smoothness",.08f);
        concrete.SetTexture("_BaseMap",SurfaceTexture("Weathered paving",false)); concrete.SetTextureScale("_BaseMap",new Vector2(10,10)); concrete.SetFloat("_Smoothness",.15f);
        glass.SetFloat("_Metallic",.4f); glass.SetFloat("_Smoothness",.78f);
        water.SetFloat("_Metallic",.35f); water.SetFloat("_Smoothness",.85f);
        root = new GameObject("KAOHSIUNG — Harbor Frequency").transform;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.37f,.44f,.53f);
        RenderSettings.fog = true; RenderSettings.fogColor = new Color(.12f,.19f,.27f);
        RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = .006f;
        var sun = new GameObject("Sunset key light").AddComponent<Light>();
        sun.type = LightType.Directional; sun.color = new Color(1,.75f,.51f); sun.intensity = 1.6f;
        sun.transform.rotation = Quaternion.Euler(25,-40,0); sun.shadows = LightShadows.Soft;
        RenderSettings.sun=sun;
        var sky=Mat("Harbor atmospheric sky",Color.white); sky.shader=Shader.Find("Skybox/Procedural");
        sky.SetFloat("_SunDisk",2); sky.SetFloat("_SunSize",.045f); sky.SetFloat("_AtmosphereThickness",1.25f);
        sky.SetColor("_SkyTint",new Color(.43f,.51f,.63f)); sky.SetColor("_GroundColor",new Color(.34f,.32f,.29f)); sky.SetFloat("_Exposure",1.15f);
        RenderSettings.skybox=sky;
        var cam = new GameObject("Main Camera").AddComponent<Camera>();
        cam.tag = "MainCamera"; cam.transform.position = new Vector3(0,10,-28);
        cam.transform.LookAt(new Vector3(0,5,20)); cam.fieldOfView = 60;
        cam.clearFlags = CameraClearFlags.Skybox; cam.backgroundColor = new Color(.095f,.15f,.24f);
        cam.farClipPlane = 250; cam.gameObject.AddComponent<AudioListener>();
        Box("Harbor water", new Vector3(0,-1.15f,35), new Vector3(180,.4f,160), water);
        Box("Pier", new Vector3(0,-.5f,24), new Vector3(13,1,76), concrete);
        Box("Performance road", new Vector3(0,.04f,24), new Vector3(9,.12f,76), navy);
        for (int side = -1; side <= 1; side += 2)
        {
            Box("Neon pier edge", new Vector3(side*6.2f,.09f,24), new Vector3(.12f,.1f,76), cyan);
            Box("Track lane", new Vector3(side*3.7f,.15f,24), new Vector3(.07f,.06f,76), gold);
            for (int z = -10; z < 62; z += 5)
            {
                Box("Quay bollard", new Vector3(side*5.6f,.5f,z), new Vector3(.25f,1,.25f), navy);
                Box("Bollard lamp", new Vector3(side*5.6f,1,z), new Vector3(.3f,.15f,.3f), gold);
            }
        }
        for (int z = -10; z < 60; z += 4) Box("Road rhythm mark", new Vector3(0,.15f,z), new Vector3(.08f,.03f,1.7f), concrete);
        // Stylized Pier-2 warehouses with repeated roof bays, shutters and public art.
        for (int i = 0; i < 4; i++) Warehouse(new Vector3(-17,0,6+i*15), i);
        Box("Warehouse plaza", new Vector3(-19,-.45f,26), new Vector3(22,1,75), concrete);
        for (int i = 0; i < 5; i++)
        {
            var art = Primitive("Contemporary stacked sculpture", PrimitiveType.Cube, new Vector3(-10,1.4f+i*.8f,18), new Vector3(1.8f,.5f,1.8f), i%2==0 ? pink : cyan);
            art.transform.rotation = Quaternion.Euler(0,i*24,0);
        }
        WorldText("PIER-2 / ART DISTRICT", new Vector3(-15,5,4), .15f, gold);
        // Hexagonal facade modules evoke the music center; intentionally stylized.
        Box("Music center island", new Vector3(23,-.4f,34), new Vector3(24,1,40), concrete);
        Box("Music center podium",new Vector3(21,1.2f,32),new Vector3(18,2.4f,12),concrete);
        Box("Music center rear volume",new Vector3(21,7.5f,32),new Vector3(12,11,7),steel);
        for(int i=0;i<13;i++)
        {
            Box("Lobby glazing",new Vector3(13+i*1.3f,1.5f,25.95f),new Vector3(1.15f,2,.12f),glass);
            Box("Lobby mullion",new Vector3(12.4f+i*1.3f,1.5f,25.85f),new Vector3(.08f,2.4f,.15f),steel);
        }
        for(int i=0;i<4;i++) Box("Entrance steps",new Vector3(21,.12f+i*.12f,24.3f+i*.4f),new Vector3(10,.24f,.6f),concrete);
        for (int col = 0; col < 6; col++)
        for (int row = 0; row < 5; row++)
        {
            if ((col == 0 || col == 5) && row > 2) continue;
            Vector3 p = new Vector3(14+col*2.35f,2.1f+row*2.6f+(col%2)*1.3f,27+Mathf.Abs(col-2)*.6f);
            Hex("Honeycomb music facade", p, 1.48f, 2.5f, glass);
            var ring = new GameObject("Honeycomb luminous rim").AddComponent<LineRenderer>();
            ring.transform.SetParent(root); ring.positionCount = 6; ring.loop = true;
            ring.sharedMaterial = (row+col)%7==0 ? window : steel; ring.widthMultiplier = .065f;
            for (int k = 0; k < 6; k++)
            {
                float a = Mathf.Deg2Rad*(60*k);
                ring.SetPosition(k, p + new Vector3(Mathf.Cos(a)*1.48f,Mathf.Sin(a)*1.48f,-1.3f));
            }
        }
        WorldText("KAOHSIUNG MUSIC CENTER", new Vector3(21,16,26), .16f, gold);
        // Cargo cranes, old railway and hill silhouettes form the port backdrop.
        for (int i = 0; i < 3; i++)
        {
            float x = -37+i*13;
            Box("Cargo crane mast", new Vector3(x,8,67), new Vector3(.8f,16,.8f), gold);
            Box("Cargo crane boom", new Vector3(x+4,16,67), new Vector3(19,.65f,.65f), gold);
            Box("Crane cable", new Vector3(x+11,11,67), new Vector3(.08f,10,.08f), navy);
        }
        for (int s = -1; s <= 1; s+=2) Box("Historical rail", new Vector3(-25+s*.7f,.13f,26), new Vector3(.09f,.1f,75), navy);
        for (int i = 0; i < 30; i++) Box("Railway sleeper", new Vector3(-25,.07f,-10+i*2.5f), new Vector3(2.4f,.12f,.2f), roof);
        WorldText("TAKAO / OLD PORT RAILWAY", new Vector3(-25,2,0), .09f, concrete);
        BackgroundDetails();
        for (int i = 0; i < 45; i++)
        {
            float x = Mathf.Sin(i*8.1f)*70;
            Box("Water reflection", new Vector3(x,-.92f,5+i*2), new Vector3(2+i%5,.02f,.10f), i%3==0 ? gold : glass);
        }
        var vehicle = new GameObject("Harbor sound cruiser").transform; vehicle.SetParent(root); vehicle.position = new Vector3(0,.7f,-5);
        var hull = Box("Cruiser chassis", vehicle.position, new Vector3(2.1f,.45f,3.3f), navy); hull.transform.SetParent(vehicle,true);
        var cockpit = Box("Cruiser cockpit", vehicle.position+new Vector3(0,.5f,0), new Vector3(1.4f,.65f,1.5f), glass); cockpit.transform.SetParent(vehicle,true);
        for (int s=-1;s<=1;s+=2)
        {
            var trim = Box("Cruiser light strip", vehicle.position+new Vector3(s*.98f,.15f,0),new Vector3(.1f,.12f,3),cyan); trim.transform.SetParent(vehicle,true);
            for(int z=-1;z<=1;z+=2)
            {
                var wheel = Primitive("Cruiser wheel",PrimitiveType.Cylinder,vehicle.position+new Vector3(s*1.05f,-.1f,z),new Vector3(.55f,.14f,.55f),navy);
                wheel.transform.rotation = Quaternion.Euler(0,0,90); wheel.transform.SetParent(vehicle,true);
            }
        }
        // The center ring is the actual judgment position; outer waveform is decorative.
        var wave = new GameObject("Throttle wave receiver").transform; wave.SetParent(root); wave.position = new Vector3(0,3.9f,2);
        var receiver = new GameObject("Judgment ring").AddComponent<LineRenderer>(); receiver.transform.SetParent(wave,false);
        receiver.useWorldSpace=false; receiver.loop=true; receiver.positionCount=64; receiver.widthMultiplier=.09f; receiver.sharedMaterial=cyan;
        for(int i=0;i<64;i++) { float a=i*Mathf.PI*2/64; receiver.SetPosition(i,new Vector3(Mathf.Cos(a)*.65f,Mathf.Sin(a)*.65f,0)); }
        var waveLine = new GameObject("Sound waveform").AddComponent<LineRenderer>(); waveLine.transform.SetParent(root);
        waveLine.positionCount=100; waveLine.widthMultiplier=.07f; waveLine.sharedMaterial=cyan;
        for(int i=0;i<100;i++) {float x=-4.5f+9f*i/99; waveLine.SetPosition(i,new Vector3(x,3.9f+Mathf.Sin(x*3)*.14f*Mathf.Abs(x),2));}
        for(int i=0;i<6;i++) Box("Height ruler",new Vector3(-4.8f,1.4f+i,2),new Vector3(.35f,.035f,.035f),gold);
        var game = new GameObject("Harbor rhythm controller").AddComponent<HarborRhythmGame>();
        game.wave=wave; game.waveLine=waveLine; game.vehicle=vehicle; game.noteMaterial=cyan; game.goldMaterial=gold;
        game.audioSource=game.gameObject.AddComponent<AudioSource>(); game.audioSource.playOnAwake=false;
        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene,ScenePath);
        var scenes=EditorBuildSettings.scenes.Where(s=>s.path!=ScenePath).ToList(); scenes.Insert(0,new EditorBuildSettingsScene(ScenePath,true)); EditorBuildSettings.scenes=scenes.ToArray();
        AssetDatabase.SaveAssets();
        if(previous.IsValid() && !previous.isDirty) EditorSceneManager.CloseScene(previous,true);
        Selection.activeGameObject=game.gameObject;
        Capture(cam,"HarborPreview.png");
        File.WriteAllText("Assets/HarborRhythm/BuildReport.txt","Scene generated successfully: "+ScenePath+"\nJudgment boundary checks: "+ValidateJudgment()+"\nObjects: "+scene.GetRootGameObjects().Sum(g=>g.GetComponentsInChildren<Transform>().Length));
        Debug.Log("Harbor Rhythm: Kaohsiung 3D scene generated and preview captured.");
    }

    static string ValidateJudgment()
    {
        if(HarborRhythmGame.Judge(0)!="PERFECT" || HarborRhythmGame.Judge(.32f)!="PERFECT" || HarborRhythmGame.Judge(.33f)!="GOOD" || HarborRhythmGame.Judge(.72f)!="GOOD" || HarborRhythmGame.Judge(.73f)!="MISS") throw new Exception("Judgment boundary check failed");
        return "PASS";
    }
    static Material Mat(string name, Color color, float emission=0)
    {
        string path="Assets/HarborRhythm/Materials/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m) {m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.color=color; m.SetFloat("_Smoothness",.45f); m.enableInstancing=true;
        if(emission>0) {m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*emission);}
        EditorUtility.SetDirty(m); return m;
    }
    static GameObject Box(string name,Vector3 p,Vector3 scale,Material m) => Primitive(name,PrimitiveType.Cube,p,scale,m);
    static GameObject Primitive(string name,PrimitiveType type,Vector3 p,Vector3 scale,Material m)
    {
        var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(root);g.transform.position=p;g.transform.localScale=scale;
        g.GetComponent<Renderer>().sharedMaterial=m; UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;
    }
    static void Warehouse(Vector3 pos,int index)
    {
        Box("Pier-2 warehouse "+index,pos+new Vector3(0,2,0),new Vector3(11,4,11),brick);
        Box("Stone foundation",pos+new Vector3(0,.22f,0),new Vector3(11.2f,.44f,11.2f),concrete);
        for(int s=-1;s<=1;s+=2)
        {
            var r=Box("Pitched warehouse roof",pos+new Vector3(s*2.9f,4.7f,0),new Vector3(6.4f,.3f,11.6f),roof);
            r.transform.rotation=Quaternion.Euler(0,0,-s*15);
            for(int k=0;k<23;k++)
            {
                var seam=Box("Standing roof seam",pos+new Vector3(s*2.9f,4.88f,-5.5f+k*.5f),new Vector3(6.4f,.055f,.045f),steel);
                seam.transform.rotation=Quaternion.Euler(0,0,-s*15);
            }
            Box("Rain gutter",pos+new Vector3(s*5.8f,3.95f,0),new Vector3(.14f,.18f,11.7f),steel);
            Box("Drainpipe",pos+new Vector3(s*5.6f,2,-5.25f),new Vector3(.13f,4,.13f),steel);
        }
        for(int i=0;i<4;i++)
        {
            Box("Gallery door",pos+new Vector3(5.53f,1.2f,-3.5f+i*2.4f),new Vector3(.08f,2.4f,1.6f),navy);
            Box("Gallery transom",pos+new Vector3(5.59f,3.1f,-3.5f+i*2.4f),new Vector3(.06f,.35f,1.5f),gold);
            Box("Steel door jamb",pos+new Vector3(5.62f,1.2f,-4.3f+i*2.4f),new Vector3(.16f,2.5f,.08f),steel);
            Box("Window mullion",pos+new Vector3(5.64f,3.1f,-3.5f+i*2.4f),new Vector3(.1f,.45f,.06f),steel);
            for(int k=0;k<10;k++) Box("Roller shutter rib",pos+new Vector3(5.61f,.2f+k*.22f,-3.5f+i*2.4f),new Vector3(.06f,.025f,1.45f),steel);
        }
        for(int x=-4;x<=4;x+=2)
        {
            Box("Warehouse front window recess",pos+new Vector3(x,2.5f,-5.56f),new Vector3(1.4f,1.55f,.08f),steel);
            Box("Warehouse front glass",pos+new Vector3(x,2.5f,-5.62f),new Vector3(1.18f,1.32f,.05f),glass);
            Box("Window vertical frame",pos+new Vector3(x,2.5f,-5.67f),new Vector3(.055f,1.4f,.05f),steel);
            Box("Window horizontal frame",pos+new Vector3(x,2.5f,-5.68f),new Vector3(1.25f,.055f,.05f),steel);
            Box("Window stone sill",pos+new Vector3(x,1.72f,-5.7f),new Vector3(1.6f,.12f,.3f),concrete);
        }
        Box("Gallery sign board",pos+new Vector3(0,.95f,-5.57f),new Vector3(4,.62f,.09f),navy);
        WorldText("P"+(index+1)+"  /  CONTEMPORARY ART",pos+new Vector3(0,.95f,-5.65f),.07f,window);
        for(int i=0;i<3;i++) Box("Warehouse ventilation",pos+new Vector3(-3+i*3,5.55f,0),new Vector3(.7f,.55f,1.4f),steel);
    }
    static Texture2D SurfaceTexture(string name,bool bricks)
    {
        string path="Assets/HarborRhythm/Materials/"+name+".png";
        if(!File.Exists(path))
        {
            const int size=256; var t=new Texture2D(size,size,TextureFormat.RGB24,false);var pixels=new Color[size*size];
            for(int y=0;y<size;y++) for(int x=0;x<size;x++)
            {
                float noise=Mathf.PerlinNoise(x*.23f,y*.23f)*.18f+Mathf.PerlinNoise(x*.04f,y*.04f)*.13f;
                int row=y/32; bool joint=bricks && (y%32<3 || (x+(row%2)*32)%64<3);
                float value=joint?.42f:.72f+noise; pixels[y*size+x]=new Color(value,value*.97f,value*.93f);
            }
            t.SetPixels(pixels);t.Apply();File.WriteAllBytes(path,t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        }
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.wrapMode=TextureWrapMode.Repeat;
        importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static void BackgroundDetails()
    {
        // A continuous irregular silhouette replaces the original rounded placeholders.
        const int count=60;var vertices=new Vector3[(count+1)*2];var triangles=new int[count*6];
        for(int i=0;i<=count;i++)
        {
            float x=-130+i*4.4f;float h=8+Mathf.PerlinNoise(i*.12f,1.8f)*18+Mathf.Sin(i*.27f)*5;
            vertices[i*2]=new Vector3(x,-1,118);vertices[i*2+1]=new Vector3(x,h,118);
            if(i<count){int a=i*2,j=i*6;triangles[j]=a;triangles[j+1]=a+1;triangles[j+2]=a+2;triangles[j+3]=a+2;triangles[j+4]=a+1;triangles[j+5]=a+3;}
        }
        var mesh=new Mesh{ name="Harbor mountain ridge",vertices=vertices,triangles=triangles};mesh.RecalculateNormals();
        string ridgePath="Assets/HarborRhythm/Materials/Mountain ridge.asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(ridgePath);
        if(saved){EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);}else{AssetDatabase.CreateAsset(mesh,ridgePath);saved=mesh;}
        var ridge=new GameObject("Layered mountain silhouette");ridge.transform.SetParent(root);ridge.AddComponent<MeshFilter>().sharedMesh=saved;
        ridge.AddComponent<MeshRenderer>().sharedMaterial=Mat("Mountain haze",new Color(.24f,.31f,.35f));
        for(int i=0;i<22;i++)
        {
            float x=-85+i*8, h=7+(i*7%19);float z=91+i%3*5;
            Box("Harbor skyline tower",new Vector3(x,h*.5f,z),new Vector3(5,h,5),stone);
            Box("Tower roof equipment",new Vector3(x,h+.4f,z),new Vector3(2,.8f,2),steel);
            for(int floor=1;floor<h-1;floor+=3) for(int c=-1;c<=1;c++)
                Box("Skyline window",new Vector3(x+c*1.4f,floor,z-2.55f),new Vector3(.7f,.8f,.06f),(floor+i+c)%4==0?window:glass);
        }
        // Recognizable tapered high-rise silhouette without claiming a measured reconstruction.
        Box("Port landmark tower base",new Vector3(48,9,94),new Vector3(13,18,8),stone);
        for(int s=-1;s<=1;s+=2)Box("Landmark twin tower",new Vector3(48+s*4,21,94),new Vector3(4,24,7),glass);
        Box("Landmark central crown",new Vector3(48,31,94),new Vector3(4,12,7),steel);
        Box("Landmark antenna",new Vector3(48,40,94),new Vector3(.3f,7,.3f),steel);
        for(int i=0;i<18;i++)
        {
            float x=-47+i%6*4.2f,z=71+i/6*3;
            Box("Stacked freight container",new Vector3(x,1.2f+(i%3)*2.2f,z),new Vector3(3.8f,2.1f,2.5f),i%3==0?brick:i%3==1?glass:steel);
        }
        for(int i=0;i<2;i++)
        {
            Vector3 p=new Vector3(i==0?43:-48,.15f,i==0?47:32);
            var hull=Primitive("Harbor vessel hull",PrimitiveType.Capsule,p,new Vector3(5,8,2),navy);hull.transform.rotation=Quaternion.Euler(90,0,0);
            Box("Vessel deck",p+Vector3.up*.9f,new Vector3(4,.25f,11),concrete);
            Box("Vessel bridge",p+new Vector3(0,2.1f,-2),new Vector3(3,2,3),steel);
            Box("Vessel bridge windows",p+new Vector3(0,2.4f,-3.55f),new Vector3(2.6f,.6f,.07f),glass);
            Box("Vessel mast",p+new Vector3(0,4,-2),new Vector3(.12f,3,.12f),steel);
        }
        for(int i=0;i<8;i++)
        {
            Vector3 p=new Vector3(i%2==0?-9:33,0,2+i*6);
            Box("Planter",p+Vector3.up*.3f,new Vector3(1.3f,.6f,1.3f),concrete);
            Primitive("Tree trunk",PrimitiveType.Cylinder,p+Vector3.up*1.7f,new Vector3(.22f,1.4f,.22f),brick);
            Primitive("Tree canopy",PrimitiveType.Sphere,p+Vector3.up*3.4f,new Vector3(2.2f,2.5f,2),foliage);
            Box("Waterfront bench",p+new Vector3(1.7f,.5f,0),new Vector3(.6f,.18f,2),roof);
            Box("Street lamp mast",p+new Vector3(0,2.3f,2.5f),new Vector3(.1f,4.6f,.1f),steel);
            Box("Street lamp arm",p+new Vector3(.4f,4.6f,2.5f),new Vector3(.9f,.09f,.12f),steel);
            Box("Street lamp luminaire",p+new Vector3(.75f,4.55f,2.5f),new Vector3(.4f,.1f,.3f),window);
        }
        for(int i=0;i<3;i++)
        {
            float x=-37+i*13;
            for(int j=0;j<6;j++)
            {
                var beam=Box("Crane diagonal truss",new Vector3(x-3+j*3,16,66.98f),new Vector3(3.8f,.16f,.18f),steel);beam.transform.rotation=Quaternion.Euler(0,0,j%2==0?23:-23);
            }
            Box("Crane cab",new Vector3(x+2,14.8f,67),new Vector3(2,1.6f,1.8f),glass);
        }
    }
    static void WorldText(string text,Vector3 p,float size,Material m)
    {
        var g=new GameObject(text);g.transform.SetParent(root);g.transform.position=p;
        var t=g.AddComponent<TextMesh>();t.text=text;t.fontSize=64;t.characterSize=size;t.anchor=TextAnchor.MiddleCenter;t.color=m.color;
    }
    static void Hex(string name,Vector3 p,float radius,float depth,Material mat)
    {
        var vertices=new Vector3[14];var tris=new System.Collections.Generic.List<int>();
        for(int j=0;j<2;j++)
        {
            vertices[j*7]=new Vector3(0,0,(j-.5f)*depth);
            for(int i=0;i<6;i++) {float a=Mathf.Deg2Rad*(60*i);vertices[j*7+i+1]=new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,(j-.5f)*depth);}
            for(int i=0;i<6;i++) {int a=j*7+i+1,b=j*7+(i+1)%6+1;if(j==0)tris.AddRange(new[]{j*7,b,a});else tris.AddRange(new[]{j*7,a,b});}
        }
        for(int i=0;i<6;i++){int a=i+1,b=(i+1)%6+1;tris.AddRange(new[]{a,b,a+7,b,b+7,a+7});}
        var mesh=new Mesh();mesh.name="Hexagonal facade module";mesh.vertices=vertices;mesh.triangles=tris.ToArray();mesh.RecalculateNormals();
        // Persist geometry so the saved scene survives editor reloads and builds.
        string path="Assets/HarborRhythm/Materials/Honeycomb.asset";
        var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(!saved){AssetDatabase.CreateAsset(mesh,path);saved=mesh;}else UnityEngine.Object.DestroyImmediate(mesh);
        var g=new GameObject(name);g.transform.SetParent(root);g.transform.position=p;g.AddComponent<MeshFilter>().sharedMesh=saved;g.AddComponent<MeshRenderer>().sharedMaterial=mat;
    }
    public static void Capture(Camera cam,string filename)
    {
        var rt=new RenderTexture(1280,720,24);var old=RenderTexture.active;var target=cam.targetTexture;
        cam.targetTexture=rt;
        if (GraphicsSettings.currentRenderPipeline != null)
            RenderPipeline.SubmitRenderRequest(cam, new UniversalRenderPipeline.SingleCameraRequest { destination = rt });
        else cam.Render();
        RenderTexture.active=rt;
        var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();
        File.WriteAllBytes("Assets/HarborRhythm/"+filename,tex.EncodeToPNG());
        cam.targetTexture=target;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(tex);rt.Release();UnityEngine.Object.DestroyImmediate(rt);
    }
}

// Consume an explicit local build request after Unity's script compilation.
[InitializeOnLoad]
public static class HarborBuildRequest
{
    static HarborBuildRequest(){EditorApplication.delayCall+=TryBuild;}
    static void TryBuild()
    {
        const string request="Assets/HarborRhythm/build.request";
        if(!File.Exists(request))return;
        if(EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.delayCall+=TryBuild;return;}
        try{HarborSceneBuilder.Build();File.Delete(request);}
        catch(Exception e){Debug.LogException(e);File.WriteAllText("Assets/HarborRhythm/BuildReport.txt",e.ToString());File.Delete(request);}
    }
}
