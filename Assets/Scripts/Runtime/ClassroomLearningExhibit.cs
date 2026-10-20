using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>Builds clear, readable, explorable science exhibits from Unity primitives when Classroom loads.</summary>
public static class ClassroomLearningExhibit
{
    public static string ActiveDescription {get;private set;}="";
    public static string ActiveModel {get;private set;}="";
    public static bool HasActiveModel=>activeModelRoot!=null;
    private static Transform root;
    private static Transform hostRoot;
    private static Transform activeModelRoot;
    private static Material deepBlue;
    private static Material cyan;
    private static Material coral;
    private static Material gold;
    private static Material white;
    private static Material green;
    private static Material violet;
    private static Material cellWall;

    public static void Ensure()
    {
        GameObject existing = GameObject.Find("Classroom_Learning_Exhibits");
        if (existing != null)
        {
            hostRoot = root = existing.transform;
            if (deepBlue == null) MakeMaterials();
            return;
        }
        MakeMaterials();ActiveDescription="";ActiveModel="";
        GameObject rootObject = new GameObject("Classroom_Learning_Exhibits");
        hostRoot = root = rootObject.transform;
    }

    public static bool ShowModel(string requested)
    {
        if (string.IsNullOrWhiteSpace(requested)) return false;
        string topic = ResolveModelName(requested);
        if (string.IsNullOrEmpty(topic)) return false;
        Ensure();
        if(LearningModelInspection.Current!=null){LearningModelInspection.Current.Exit();Object.Destroy(LearningModelInspection.Current.gameObject);}
        else if(activeModelRoot!=null)Object.Destroy(activeModelRoot.gameObject);
        ActiveDescription="";
        GameObject model = new GameObject("ActiveLearningModel_" + topic.Replace(' ', '_'));
        model.transform.SetParent(hostRoot, false);
        activeModelRoot = model.transform;
        root = activeModelRoot;
        Vector3 station = GetTeachingPosition();

        switch (topic)
        {
            case "Atom": BuildAtomicModel(station); break;
            case "Solar System":
                if (!BlenderWorldCollection.ShowOrrery(root, station)) BuildSolarSystem(station);
                break;
            case "Plant Cell": BuildPlantCell(station); break;
            case "Animal Cell": BuildAnimalCell(station); break;
            case "DNA": BuildDna(station); break;
            case "Human Heart": BuildHeart(station); break;
            case "Water Cycle": BuildWaterCycle(station); break;
            case "Electric Circuit": BuildCircuit(station); break;
            case "Water Molecule": BuildWaterMolecule(station); break;
            case "Volcano": BuildVolcano(station); break;
            case "Pythagorean Theorem": BuildPythagoreanModel(station); break;
            case "Plant": BuildPlant(station); break;
            case "Animal Anatomy": BuildAnimalAnatomy(station); break;
            case "Sunflower": BuildSunflower(station); break;
            case "Oak Tree": BuildOakTree(station); break;
            case "Light Spectrum": BuildLightSpectrum(station); break;
            case "Periodic Table": BuildPeriodicTable(station); break;
            case "Triceratops": BuildImportedLearningModel(station, "Triceratops", "Paleontology model",
                "The Smithsonian's Triceratops horridus model represents a large, horned herbivore from the Late Cretaceous. Its skull frill and three horns are real anatomical features; the digital model is an interpretive specimen, not a live animal.",
                "biology, dinosaur, paleontology, triceratops, smithsonian-open-access", "SmithsonianTriceratops", 2.45f, new Color(0.70f, 0.46f, 0.32f)); break;
            case "Fern": BuildImportedLearningModel(station, "Fern", "Plant diversity model",
                "A three-dimensional Cyathea furfuracea fern specimen from the Smithsonian collection. Ferns reproduce with spores rather than flowers or seeds, and their fronds unroll as they grow.",
                "biology, botany, fern, plant, spores, smithsonian-open-access", "SmithsonianFern", 2.25f, new Color(0.35f, 0.68f, 0.40f)); break;
            case "Apollo 11": BuildImportedLearningModel(station, "Apollo 11 Command Module", "Spaceflight engineering model",
                "The Apollo 11 Command Module carried Neil Armstrong, Buzz Aldrin, and Michael Collins. It returned the crew to Earth in July 1969; this Smithsonian Open Access model invites close inspection of its compact spacecraft form.",
                "space, engineering, apollo 11, nasa, smithsonian-open-access", "SmithsonianApollo11CommandModule", 2.25f, new Color(0.68f, 0.69f, 0.67f)); break;
            default: BuildPlanet(station, topic); break;
        }
        root = hostRoot;
        foreach(var facts in activeModelRoot.GetComponentsInChildren<ObjectMetadata>())
            if(!string.IsNullOrWhiteSpace(facts.description)){ActiveDescription=facts.description;break;}
        ActiveModel=topic;
        ExplorationNotebook.RecordExhibit(topic);
        foreach(var label in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude))
        {
            if(label.text!=null&&(label.text.IndexOf("Newton",System.StringComparison.OrdinalIgnoreCase)>=0||label.name=="ActiveLessonBoard"))
            {
                int stop=ActiveDescription.IndexOf(". ",System.StringComparison.Ordinal);
                string shortExplanation=stop>=0?ActiveDescription.Substring(0,stop+1):ActiveDescription;
                if(shortExplanation.Length>180)shortExplanation=shortExplanation.Substring(0,177)+"...";
                label.name="ActiveLessonBoard";label.text=topic+"\n"+shortExplanation+"\nSimplified teaching representation; not to scale.";
                label.rectTransform.sizeDelta=new Vector2(6.6f,1.65f);label.alignment=TextAlignmentOptions.Center;
                label.textWrappingMode=TextWrappingModes.Normal;label.enableAutoSizing=true;label.fontSizeMin=1.8f;label.fontSizeMax=2.4f;label.overflowMode=TextOverflowModes.Ellipsis;
            }
        }
        LearningModelInspection.Attach(activeModelRoot,station+Vector3.up*1.35f);
        var guide=Object.FindAnyObjectByType<ScenarioCharacterIdentity>();
        if(guide!=null&&Vector3.Distance(guide.transform.position,station)<1.7f)
        {
            var nav=guide.GetComponent<UnityEngine.AI.NavMeshAgent>();
            Vector3 offset=Camera.main!=null?Camera.main.transform.right:Vector3.right;offset.y=0;
            if(nav!=null&&nav.enabled&&nav.isOnNavMesh&&UnityEngine.AI.NavMesh.SamplePosition(station+offset.normalized*2.2f,out var safe,1.5f,UnityEngine.AI.NavMesh.AllAreas))nav.Warp(safe.position);
        }
        if(guide!=null)
        {
            Vector3 side=Camera.main!=null?Camera.main.transform.right:Vector3.right;side.y=0;
            guide.GetComponent<AgentCompanionController>()?.TryVoiceDestination(station+side.normalized*2.2f);
        }
        return true;
    }

    private static string ResolveModelName(string requested)
    {
        string query = requested.Trim().ToLowerInvariant().Replace(" exhibit", "");
        string[] topics = { "apollo 11", "command module", "triceratops", "dinosaur", "fern", "mercury", "venus", "earth", "mars", "jupiter", "saturn", "uranus", "neptune", "solar system", "atom", "periodic table", "plant cell", "animal cell", "animal anatomy", "sunflower", "oak tree", "dna", "human heart", "water cycle", "electric circuit", "water molecule", "volcano", "pythagorean theorem", "plant", "light spectrum" };
        // Specific names win over substrings: anatomy contains atom.
        System.Array.Sort(topics,(a,b)=>b.Length.CompareTo(a.Length));
        foreach (string topic in topics)
            if (query == topic || query.Contains(topic))
            {
                if (topic == "dna") return "DNA";
                if (topic == "command module") return "Apollo 11";
                if (topic == "dinosaur") return "Triceratops";
                return System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(topic);
            }
        if (query.Contains("animal")) return "Animal Anatomy";
        if (query.Contains("oak") || query.Contains("acorn")) return "Oak Tree";
        if (query.Contains("cell")) return "Plant Cell";
        if (query.Contains("planet")) return "Solar System";
        return string.Empty;
    }

    private static Vector3 GetTeachingPosition()
    {
        Transform board = FindTransformContains("BoardFrame");
        Vector3 center = board != null ? board.position : Vector3.zero;
        Vector3 towardsLearners = Vector3.forward;
        Camera camera = Camera.main;
        if (camera != null)
        {
            Vector3 fromBoardToCamera = camera.transform.position - center;
            fromBoardToCamera.y = 0f;
            if (fromBoardToCamera.sqrMagnitude > 0.05f) towardsLearners = fromBoardToCamera.normalized;
        }
        Vector3 right = Vector3.Cross(Vector3.up, towardsLearners).normalized;
        return center + towardsLearners * 1.75f + Vector3.up * 0.1f + right * 0.15f;
    }

    private static void BuildAtomicModel(Vector3 basePosition)
    {
        Transform exhibit = CreateExhibit("Atom Exhibit", "Atom", "Atomic model",
            "A simplified lithium-7 model: three protons and four neutrons in the nucleus, with three electrons shown in a two-plus-one arrangement. Orbit paths are a teaching diagram only; electrons are described more accurately by quantum probability distributions, not tiny planets following fixed tracks.",
            "atom, science, chemistry, classroom, learning-model", basePosition, new Color(0.17f, 0.58f, 0.77f));
        Vector3 center = new Vector3(0f, 1.22f, 0f);
        for (int i = 0; i < 3; i++)
        {
            float angle = i * Mathf.PI * 0.5f;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), (i % 2 == 0 ? 0.11f : -0.11f));
            MakeSphere(exhibit, "Proton", center + offset * 0.15f, Vector3.one * 0.24f, coral);
        }
        for (int i = 0; i < 4; i++)
        {
            float angle = i * Mathf.PI * 0.5f + 0.4f;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), (i % 2 == 0 ? -0.13f : 0.13f));
            MakeSphere(exhibit, "Neutron", center + offset * 0.15f, Vector3.one * 0.23f, white);
        }
        Vector3 planeA = new Vector3(1f, 0f, 0f);
        Vector3 planeB = new Vector3(0f, 1f, 0f);
        AddOrbitLine(exhibit, center, 0.62f, planeA, planeB, cyan, "Inner electron cloud guide");
        AddOrbitLine(exhibit, center, 0.94f, Vector3.forward, planeB, violet, "Outer electron cloud guide");
        OrbitingLearningModel motion = exhibit.gameObject.AddComponent<OrbitingLearningModel>();
        motion.AddOrbiter(MakeSphere(exhibit, "Electron", center, Vector3.one * 0.12f, cyan).transform, center, 0.62f, Vector3.up, 2.2f, 0f);
        motion.AddOrbiter(MakeSphere(exhibit, "Electron", center, Vector3.one * 0.12f, cyan).transform, center, 0.62f, Vector3.up, 2.2f, Mathf.PI);
        motion.AddOrbiter(MakeSphere(exhibit, "Electron", center, Vector3.one * 0.12f, violet).transform, center, 0.94f, Vector3.forward, 1.25f, 0.65f);
        AddLabel(exhibit, "ATOM · lithium-7", new Vector3(0f, 2.05f, 0f));
    }

    private static void BuildSolarSystem(Vector3 basePosition)
    {
        Transform exhibit = CreateExhibit("Solar System Exhibit", "Solar System", "Astronomy model",
            "An illustrative model of the Solar System. The Sun contains most of the system’s mass; the eight planets orbit it, while distances and sizes here are compressed dramatically so the model fits in a classroom. Earth is the third planet from the Sun.",
            "solar-system, astronomy, planets, classroom, learning-model", basePosition, new Color(0.81f, 0.55f, 0.28f));
        Vector3 center = new Vector3(0f, 1.32f, 0f);
        MakeSphere(exhibit, "Sun", center, Vector3.one * 0.42f, gold);
        float[] radii = { 0.54f, 0.73f, 0.93f, 1.13f, 1.42f, 1.68f, 1.92f, 2.14f };
        float[] sizes = { 0.09f, 0.15f, 0.16f, 0.12f, 0.30f, 0.26f, 0.23f, 0.22f };
        float[] speeds = { 2.1f, 1.65f, 1.35f, 1.1f, 0.68f, 0.48f, 0.36f, 0.29f };
        Color[] colors = { white.color, new Color(0.91f, 0.67f, 0.31f), new Color(0.25f, 0.61f, 0.93f), new Color(0.78f, 0.35f, 0.25f), new Color(0.80f, 0.62f, 0.47f), new Color(0.84f, 0.74f, 0.54f), cyan.color, new Color(0.26f, 0.39f, 0.86f) };
        string[] names = { "Mercury", "Venus", "Earth", "Mars", "Jupiter", "Saturn", "Uranus", "Neptune" };
        OrbitingLearningModel motion = exhibit.gameObject.AddComponent<OrbitingLearningModel>();
        for (int i = 0; i < radii.Length; i++)
        {
            AddOrbitLine(exhibit, center, radii[i], Vector3.right, Vector3.forward, i % 2 == 0 ? cyan : violet, names[i] + " orbit guide");
            SphereCollider body = MakeSphere(exhibit, names[i], center + new Vector3(radii[i], 0f, 0f), Vector3.one * sizes[i], MakeMaterial(names[i], colors[i]));
            motion.AddOrbiter(body.transform, center, radii[i], Vector3.up, speeds[i], i * 0.73f);
        }
        AddLabel(exhibit, "SOLAR SYSTEM · not to scale", new Vector3(0f, 2.03f, 0f));
    }

    private static void BuildPlantCell(Vector3 basePosition)
    {
        Transform exhibit = CreateExhibit("Plant Cell Exhibit", "Plant Cell", "Biology model",
            "A simplified plant-cell model. The cell wall provides support, the membrane regulates exchange, chloroplasts capture light for photosynthesis, and the nucleus contains DNA. The organelles are enlarged and arranged for visibility, not to scale.",
            "plant-cell, biology, photosynthesis, classroom, learning-model", basePosition, new Color(0.39f, 0.68f, 0.43f));
        Vector3 center = new Vector3(0f, 1.20f, 0f);
        SphereCollider body = MakeSphere(exhibit, "Cell Wall", center, new Vector3(1.58f, 1.04f, 0.82f), cellWall);
        body.isTrigger = true;
        SphereCollider membrane = MakeSphere(exhibit, "Cell Membrane", center, new Vector3(1.42f, 0.90f, 0.70f), LumenRuntimeResources.Own(new Material(green) { color = new Color(0.36f, 0.78f, 0.49f, 0.55f) },root.gameObject));
        membrane.isTrigger = true;
        MakeSphere(exhibit, "Nucleus", center + new Vector3(-0.23f, 0.08f, 0.04f), Vector3.one * 0.34f, violet);
        MakeSphere(exhibit, "Vacuole", center + new Vector3(0.34f, -0.08f, 0.03f), new Vector3(0.56f, 0.28f, 0.35f), cyan);
        Vector3[] chloroplasts = { new Vector3(-0.46f, 0.22f, 0.20f), new Vector3(0.43f, 0.20f, 0.20f), new Vector3(-0.42f, -0.20f, 0.16f), new Vector3(0.42f, -0.22f, 0.16f) };
        foreach (Vector3 offset in chloroplasts) MakeSphere(exhibit, "Chloroplast", center + offset, new Vector3(0.20f, 0.13f, 0.12f), green);
        AddLabel(exhibit, "PLANT CELL · enlarged", new Vector3(0f, 2.02f, 0f));
    }

    private static void BuildPlanet(Vector3 basePosition, string name)
    {
        Color color = name switch
        {
            "Mercury" => new Color(0.54f, 0.53f, 0.50f), "Venus" => new Color(0.91f, 0.66f, 0.34f),
            "Earth" => new Color(0.15f, 0.45f, 0.88f), "Mars" => new Color(0.78f, 0.30f, 0.20f),
            "Jupiter" => new Color(0.80f, 0.60f, 0.43f), "Saturn" => new Color(0.83f, 0.73f, 0.52f),
            "Uranus" => new Color(0.32f, 0.74f, 0.78f), _ => new Color(0.25f, 0.35f, 0.82f)
        };
        string description = name + " is shown as a single-planet teaching model. Its surface details, color, and size are illustrative rather than a literal photograph or scale comparison.";
        Transform exhibit = CreateExhibit(name + " Model", name, "Astronomy model", description,
            "astronomy, planet, " + name.ToLowerInvariant() + ", learning-model", basePosition, color);
        GameObject nasaEarth = name == "Earth" ? ImportedModelUtility.AddFittedModel("NasaEarth", exhibit, 1.55f, 0.78f) : null;
        Vector3 center = new Vector3(0f, nasaEarth != null ? 1.555f : 1.45f, 0f);
        if (nasaEarth == null)
        {
            SphereCollider planet = MakeSphere(exhibit, name, center, Vector3.one * 1.12f, MakeMaterial(name + " Surface", color));
            planet.isTrigger = true;
        }
        if (name == "Earth")
        {
            Material land = MakeMaterial("Earth Land", new Color(0.24f, 0.66f, 0.39f));
            float radius = nasaEarth != null ? 0.76f : 0.56f;
            float scale = radius / 0.56f;
            MakeEarthLand(exhibit, "Continental region · Americas", center, radius, -0.31f * scale, 0.15f * scale, new Vector3(0.23f, 0.37f, 0.05f) * scale, land);
            MakeEarthLand(exhibit, "Continental region · Africa", center, radius, 0.13f * scale, -0.11f * scale, new Vector3(0.19f, 0.38f, 0.05f) * scale, land);
            MakeEarthLand(exhibit, "Continental region · Asia", center, radius, 0.37f * scale, 0.16f * scale, new Vector3(0.31f, 0.27f, 0.05f) * scale, land);
            MakeEarthLand(exhibit, "Continental region · Australia", center, radius, 0.43f * scale, -0.28f * scale, new Vector3(0.15f, 0.12f, 0.05f) * scale, land);
        }
        else if (name == "Saturn")
        {
            AddOrbitLine(exhibit, center, 0.92f, Vector3.right, Vector3.forward, gold, "Saturn's rings");
            AddOrbitLine(exhibit, center, 1.16f, Vector3.right, Vector3.forward, white, "Saturn's outer ring");
        }
        else if (name == "Jupiter")
        {
            for (int i = 0; i < 5; i++)
            {
                float y = center.y - 0.42f + i * 0.20f;
                GameObject band = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                band.name = "Atmospheric band";
                band.transform.SetParent(exhibit, false);
                band.transform.localPosition = new Vector3(0f, y, 0.02f);
                band.transform.localScale = new Vector3(0.56f, 0.018f, 0.56f);
                band.GetComponent<Renderer>().sharedMaterial = MakeMaterial("Jupiter band " + i, i % 2 == 0 ? new Color(0.89f, 0.72f, 0.55f) : new Color(0.64f, 0.39f, 0.31f));
                Object.Destroy(band.GetComponent<Collider>());
            }
        }
        AddLabel(exhibit, nasaEarth != null ? "EARTH · NASA 3D MODEL" : name.ToUpperInvariant() + " · INDIVIDUAL MODEL",
            new Vector3(0f, nasaEarth != null ? 2.48f : 2.30f, 0f));
        exhibit.gameObject.AddComponent<LearningModelSpin>();
    }

    private static void BuildImportedLearningModel(Vector3 basePosition, string displayName, string category, string description, string tags, string resourceName, float maxExtent, Color accent)
    {
        Transform exhibit = CreateExhibit(displayName + " Exhibit", displayName, category, description, tags, basePosition, accent);
        GameObject model = ImportedModelUtility.AddFittedModel(resourceName, exhibit, maxExtent, 0.82f, 180f);
        if (model == null)
        {
            // Keep a clear, selectable model on screen even if Unity has not yet
            // finished generating the Resource prefab after an asset refresh.
            Material fallback = MakeMaterial(displayName + " placeholder", accent);
            if (displayName == "Fern")
            {
                for (int i = 0; i < 7; i++)
                {
                    float angle = i * Mathf.PI * 2f / 7f;
                    Vector3 leafPosition = new Vector3(Mathf.Cos(angle) * 0.35f, 1.20f + (i % 3) * 0.14f, Mathf.Sin(angle) * 0.18f);
                    MakeSphere(exhibit, "Fern frond", leafPosition, new Vector3(0.18f, 1.15f, 0.10f), fallback);
                }
            }
            else if (displayName.Contains("Triceratops"))
            {
                MakeSphere(exhibit, "Dinosaur body", new Vector3(0f, 1.18f, 0f), new Vector3(1.48f, 0.72f, 0.66f), fallback);
                MakeSphere(exhibit, "Dinosaur frill", new Vector3(0f, 1.58f, -0.58f), new Vector3(0.90f, 0.88f, 0.20f), fallback);
                MakeSphere(exhibit, "Dinosaur head", new Vector3(0f, 1.35f, -0.88f), new Vector3(0.70f, 0.52f, 0.56f), fallback);
            }
            else MakeSphere(exhibit, "Spacecraft model", new Vector3(0f, 1.48f, 0f), new Vector3(1.18f, 1.18f, 1.18f), fallback);
        }
        AddLabel(exhibit, displayName.ToUpperInvariant() + " · 3D SPECIMEN", new Vector3(0f, 3.22f, 0f));
    }

    private static void MakeEarthLand(Transform exhibit, string label, Vector3 center, float radius, float x, float y, Vector3 size, Material material)
    {
        float surfaceZ = Mathf.Sqrt(Mathf.Max(0.01f, radius * radius - x * x - y * y));
        Vector3 position = center + new Vector3(x, y, surfaceZ + 0.008f);
        MakeSphere(exhibit, label, position, size, material).isTrigger = true;
    }

    private static void BuildAnimalCell(Vector3 basePosition)
    {
        Transform exhibit = CreateExhibit("Animal Cell Model", "Animal Cell", "Biology model",
            "A simplified animal cell. The membrane encloses cytoplasm; the nucleus stores most genetic material, while mitochondria help release usable energy. The organelles are enlarged and not to scale.",
            "animal-cell, biology, mitochondria, learning-model", basePosition, new Color(0.62f, 0.40f, 0.77f));
        Vector3 center = new Vector3(0f, 1.38f, 0f);
        MakeSphere(exhibit, "Cell membrane", center, new Vector3(1.52f, 0.96f, 1.08f), LumenRuntimeResources.Own(new Material(cyan) { color = new Color(0.30f, 0.75f, 0.83f, 0.28f) },root.gameObject)).isTrigger = true;
        MakeSphere(exhibit, "Nucleus", center + new Vector3(-0.26f, 0.05f, 0.04f), Vector3.one * 0.46f, violet).isTrigger = true;
        for (int i = 0; i < 5; i++)
        {
            float angle = i * Mathf.PI * 0.4f;
            Vector3 at = center + new Vector3(Mathf.Cos(angle) * 0.48f, Mathf.Sin(angle) * 0.32f, 0.04f);
            MakeSphere(exhibit, "Mitochondrion", at, new Vector3(0.24f, 0.13f, 0.16f), coral).isTrigger = true;
        }
        AddLabel(exhibit, "ANIMAL CELL · enlarged", new Vector3(0f, 2.20f, 0f));
    }

    private static void BuildDna(Vector3 basePosition)
    {
        Transform exhibit = CreateExhibit("DNA Model", "DNA", "Molecular biology model",
            "DNA has two strands twisted into a double helix. Its base pairs are adenine–thymine and cytosine–guanine; the model enlarges the molecule dramatically so the repeating structure is visible.",
            "dna, biology, genetics, molecule, learning-model", basePosition, new Color(0.44f, 0.59f, 0.93f));
        for (int i = 0; i <= 20; i++)
        {
            float t = i / 20f * Mathf.PI * 4f;
            float y = 0.46f + i * 0.095f;
            Vector3 a = new Vector3(Mathf.Cos(t) * 0.46f, y, Mathf.Sin(t) * 0.32f);
            Vector3 b = new Vector3(Mathf.Cos(t + Mathf.PI) * 0.46f, y, Mathf.Sin(t + Mathf.PI) * 0.32f);
            MakeSphere(exhibit, "Sugar-phosphate backbone", a, Vector3.one * 0.12f, cyan).isTrigger = true;
            MakeSphere(exhibit, "Sugar-phosphate backbone", b, Vector3.one * 0.12f, violet).isTrigger = true;
            if (i % 2 == 0) AddBeam(exhibit, "Base pair", a, b, 0.055f, i % 4 == 0 ? coral : gold);
        }
        AddLabel(exhibit, "DNA · DOUBLE HELIX", new Vector3(0f, 2.75f, 0f));
    }

    private static void BuildHeart(Vector3 basePosition)
    {
        Transform exhibit = CreateExhibit("Human Heart Model", "Human Heart", "Anatomy model",
            "A simplified four-chamber heart model. The right atrium and ventricle send oxygen-poor blood toward the lungs; the left chambers send oxygen-rich blood to the body. The flow is simplified for teaching.",
            "human-heart, anatomy, biology, circulation, learning-model", basePosition, coral.color);
        Vector3 center = new Vector3(0f, 1.42f, 0f);
        MakeSphere(exhibit, "Right atrium · oxygen-poor blood", center + new Vector3(-0.30f, 0.35f, 0f), new Vector3(0.48f, 0.46f, 0.40f), cyan).isTrigger = true;
        MakeSphere(exhibit, "Right ventricle · oxygen-poor blood", center + new Vector3(-0.27f, -0.18f, 0.02f), new Vector3(0.56f, 0.58f, 0.44f), cyan).isTrigger = true;
        MakeSphere(exhibit, "Left atrium · oxygen-rich blood", center + new Vector3(0.29f, 0.35f, 0f), new Vector3(0.48f, 0.46f, 0.40f), coral).isTrigger = true;
        MakeSphere(exhibit, "Left ventricle · oxygen-rich blood", center + new Vector3(0.26f, -0.18f, 0.02f), new Vector3(0.56f, 0.58f, 0.44f), coral).isTrigger = true;
        AddBeam(exhibit, "Aorta · to body", new Vector3(0.30f, 1.70f, 0f), new Vector3(0.44f, 2.30f, 0f), 0.14f, coral);
        AddBeam(exhibit, "Pulmonary artery · to lungs", new Vector3(-0.31f, 1.70f, 0f), new Vector3(-0.46f, 2.12f, 0f), 0.13f, cyan);
        AddLabel(exhibit, "HEART · FOUR CHAMBERS", new Vector3(0f, 2.55f, 0f));
    }

    private static void BuildWaterCycle(Vector3 basePosition)
    {
        Transform exhibit = CreateExhibit("Water Cycle Model", "Water Cycle", "Earth science model",
            "The Sun warms surface water; evaporation carries water vapour upward, condensation forms clouds, and precipitation returns water to land and ocean. Runoff and infiltration complete this simplified cycle.",
            "water-cycle, earth-science, evaporation, condensation, learning-model", basePosition, cyan.color);
        Vector3 center = new Vector3(0f, 0.70f, 0f);
        GameObject ocean = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ocean.name = "Ocean · collection"; ocean.transform.SetParent(exhibit, false); ocean.transform.localPosition = center + new Vector3(0f, 0.13f, 0f); ocean.transform.localScale = new Vector3(1.65f, 0.18f, 0.72f); ocean.GetComponent<Renderer>().sharedMaterial = cyan; Object.Destroy(ocean.GetComponent<Collider>());
        GameObject mountain = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mountain.name = "Land · runoff and infiltration"; mountain.transform.SetParent(exhibit, false); mountain.transform.localPosition = center + new Vector3(0.15f, 0.43f, 0f); mountain.transform.localRotation = Quaternion.Euler(0f, 0f, -22f); mountain.transform.localScale = new Vector3(0.92f, 0.74f, 0.68f); mountain.GetComponent<Renderer>().sharedMaterial = green; Object.Destroy(mountain.GetComponent<Collider>());
        MakeSphere(exhibit, "Cloud · condensation", new Vector3(-0.1f, 1.72f, 0f), new Vector3(0.92f, 0.38f, 0.46f), white).isTrigger = true;
        AddBeam(exhibit, "Evaporation", new Vector3(-0.58f, 0.98f, 0.14f), new Vector3(-0.42f, 1.51f, 0.14f), 0.045f, gold);
        AddBeam(exhibit, "Precipitation", new Vector3(0.40f, 1.52f, 0.16f), new Vector3(0.54f, 0.98f, 0.16f), 0.045f, cyan);
        AddLabel(exhibit, "WATER CYCLE · MATTER IN MOTION", new Vector3(0f, 2.14f, 0f));
    }

    private static void BuildCircuit(Vector3 basePosition)
    {
        Transform exhibit = CreateExhibit("Electric Circuit Model", "Electric Circuit", "Physics model",
            "A battery drives current around a closed conducting loop. The lamp lights when the switch completes the path; an open switch interrupts current. This demonstration represents a simple idealized circuit.",
            "electric-circuit, physics, electricity, battery, learning-model", basePosition, gold.color);
        GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube); board.name = "Circuit board"; board.transform.SetParent(exhibit, false); board.transform.localPosition = new Vector3(0f, 0.92f, 0f); board.transform.localScale = new Vector3(1.72f, 0.10f, 0.94f); board.GetComponent<Renderer>().sharedMaterial = deepBlue; Object.Destroy(board.GetComponent<Collider>());
        GameObject battery = GameObject.CreatePrimitive(PrimitiveType.Cube); battery.name = "Battery"; battery.transform.SetParent(exhibit, false); battery.transform.localPosition = new Vector3(-0.58f, 1.18f, 0f); battery.transform.localScale = new Vector3(0.34f, 0.42f, 0.28f); battery.GetComponent<Renderer>().sharedMaterial = violet; Object.Destroy(battery.GetComponent<Collider>());
        GameObject lamp = GameObject.CreatePrimitive(PrimitiveType.Sphere); lamp.name = "Lamp"; lamp.transform.SetParent(exhibit, false); lamp.transform.localPosition = new Vector3(0.52f, 1.25f, 0f); lamp.transform.localScale = Vector3.one * 0.30f; lamp.GetComponent<Renderer>().sharedMaterial = gold; Object.Destroy(lamp.GetComponent<Collider>());
        AddBeam(exhibit, "Circuit wire", new Vector3(-0.58f, 1.18f, 0.12f), new Vector3(-0.58f, 1.68f, 0.12f), 0.035f, coral);
        AddBeam(exhibit, "Circuit wire", new Vector3(-0.58f, 1.68f, 0.12f), new Vector3(0.52f, 1.68f, 0.12f), 0.035f, coral);
        AddBeam(exhibit, "Circuit wire", new Vector3(0.52f, 1.68f, 0.12f), new Vector3(0.52f, 1.25f, 0.12f), 0.035f, coral);
        AddBeam(exhibit, "Circuit return", new Vector3(0.52f, 1.10f, 0.12f), new Vector3(-0.58f, 1.10f, 0.12f), 0.035f, cyan);
        AddLabel(exhibit, "CLOSED CIRCUIT · BATTERY → LAMP", new Vector3(0f, 2.08f, 0f));
    }

    private static void BuildWaterMolecule(Vector3 basePosition)
    {
        Transform exhibit = CreateExhibit("Water Molecule Model", "Water Molecule", "Chemistry model",
            "One water molecule contains two hydrogen atoms covalently bonded to one oxygen atom. Its bent shape gives it a polar charge distribution; ball-and-stick sizes and bond lengths are not to scale.",
            "water-molecule, chemistry, atom, h2o, learning-model", basePosition, cyan.color);
        Vector3 oxygen = new Vector3(0f, 1.43f, 0f);
        MakeSphere(exhibit, "Oxygen atom", oxygen, Vector3.one * 0.70f, coral).isTrigger = true;
        Vector3 h1 = oxygen + new Vector3(-0.58f, 0.20f, 0f);
        Vector3 h2 = oxygen + new Vector3(0.58f, 0.20f, 0f);
        MakeSphere(exhibit, "Hydrogen atom", h1, Vector3.one * 0.39f, white).isTrigger = true;
        MakeSphere(exhibit, "Hydrogen atom", h2, Vector3.one * 0.39f, white).isTrigger = true;
        AddBeam(exhibit, "Covalent bond", oxygen, h1, 0.13f, gold);
        AddBeam(exhibit, "Covalent bond", oxygen, h2, 0.13f, gold);
        AddLabel(exhibit, "H₂O · BENT MOLECULAR SHAPE", new Vector3(0f, 2.12f, 0f));
    }

    private static void BuildVolcano(Vector3 basePosition)
    {
        Transform exhibit = CreateExhibit("Volcano Model", "Volcano", "Earth science model",
            "This cutaway volcano shows magma rising through a conduit and erupting at the crater. Real volcanoes vary: lava viscosity, dissolved gases, and tectonic setting affect eruption style.",
            "volcano, geology, magma, earth-science, learning-model", basePosition, new Color(0.68f, 0.31f, 0.23f));
        GameObject cone = GameObject.CreatePrimitive(PrimitiveType.Cylinder); cone.name = "Volcanic cone"; cone.transform.SetParent(exhibit, false); cone.transform.localPosition = new Vector3(0f, 1.10f, 0f); cone.transform.localScale = new Vector3(1.20f, 0.72f, 1.20f); cone.GetComponent<Renderer>().sharedMaterial = MakeMaterial("Volcanic rock", new Color(0.32f, 0.25f, 0.23f)); Object.Destroy(cone.GetComponent<Collider>());
        GameObject crater = GameObject.CreatePrimitive(PrimitiveType.Cylinder); crater.name = "Crater"; crater.transform.SetParent(exhibit, false); crater.transform.localPosition = new Vector3(0f, 1.78f, 0f); crater.transform.localScale = new Vector3(0.35f, 0.08f, 0.35f); crater.GetComponent<Renderer>().sharedMaterial = coral; Object.Destroy(crater.GetComponent<Collider>());
        AddBeam(exhibit, "Magma conduit", new Vector3(0f, 0.75f, 0.13f), new Vector3(0f, 1.69f, 0.13f), 0.11f, coral);
        MakeSphere(exhibit, "Ash plume", new Vector3(0.18f, 2.10f, 0f), Vector3.one * 0.28f, white).isTrigger = true;
        AddLabel(exhibit, "VOLCANO · MAGMA TO LAVA", new Vector3(0f, 2.48f, 0f));
    }

    private static void BuildPythagoreanModel(Vector3 basePosition)
    {
        Transform exhibit = CreateExhibit("Pythagorean Theorem Model", "Pythagorean Theorem", "Mathematics model",
            "For a right triangle, a² + b² = c². The three attached squares show that the combined area on the two shorter sides equals the area on the hypotenuse; lengths and colors are schematic.",
            "math, geometry, pythagorean-theorem, triangle, learning-model", basePosition, new Color(0.37f, 0.62f, 0.85f));
        GameObject triangle = new GameObject("Right triangle"); triangle.transform.SetParent(exhibit, false); triangle.transform.localPosition = new Vector3(-0.56f, 0.82f, 0f);
        Mesh mesh = LumenRuntimeResources.Own(new Mesh { name = "Right triangle surface" },root.gameObject);
        mesh.vertices = new[] { Vector3.zero, new Vector3(0f, 1.12f, 0f), new Vector3(1.48f, 0f, 0f) };
        mesh.triangles = new[] { 0, 1, 2 };
        mesh.RecalculateNormals();
        triangle.AddComponent<MeshFilter>().sharedMesh = mesh;
        triangle.AddComponent<MeshRenderer>().sharedMaterial = MakeMaterial("Triangle cyan", new Color(0.24f, 0.73f, 0.80f));
        GameObject squareA = GameObject.CreatePrimitive(PrimitiveType.Cube); squareA.name = "Square on side a"; squareA.transform.SetParent(exhibit, false); squareA.transform.localPosition = new Vector3(-0.86f, 1.38f, 0.04f); squareA.transform.localScale = new Vector3(0.53f, 0.53f, 0.08f); squareA.GetComponent<Renderer>().sharedMaterial = cyan; Object.Destroy(squareA.GetComponent<Collider>());
        GameObject squareB = GameObject.CreatePrimitive(PrimitiveType.Cube); squareB.name = "Square on side b"; squareB.transform.SetParent(exhibit, false); squareB.transform.localPosition = new Vector3(0.47f, 0.65f, 0.04f); squareB.transform.localScale = new Vector3(0.53f, 0.53f, 0.08f); squareB.GetComponent<Renderer>().sharedMaterial = gold; Object.Destroy(squareB.GetComponent<Collider>());
        AddLabel(exhibit, "a² + b² = c²", new Vector3(0f, 2.12f, 0f));
    }

    private static void BuildPlant(Vector3 basePosition)
    {
        Transform exhibit = CreateExhibit("Plant Anatomy Model", "Plant", "Botany model",
            "This specimen highlights roots, stem, leaves, and flower. Roots take up water and minerals; the stem supports the plant and transports materials; leaves capture light for photosynthesis.",
            "plant, botany, leaf, roots, stem, biology, learning-model", basePosition, green.color);
        AddBeam(exhibit, "Stem", new Vector3(0f, 0.54f, 0f), new Vector3(0f, 1.78f, 0f), 0.10f, green);
        AddBeam(exhibit, "Root", new Vector3(0f, 0.64f, 0f), new Vector3(-0.47f, 0.29f, 0.04f), 0.045f, white);
        AddBeam(exhibit, "Root", new Vector3(0f, 0.64f, 0f), new Vector3(0.47f, 0.29f, -0.04f), 0.045f, white);
        MakeSphere(exhibit, "Leaf", new Vector3(-0.32f, 1.10f, 0f), new Vector3(0.62f, 0.28f, 0.10f), green).isTrigger = true;
        MakeSphere(exhibit, "Leaf", new Vector3(0.33f, 1.45f, 0f), new Vector3(0.62f, 0.28f, 0.10f), green).isTrigger = true;
        MakeSphere(exhibit, "Flower", new Vector3(0f, 1.94f, 0f), Vector3.one * 0.30f, coral).isTrigger = true;
        AddLabel(exhibit, "PLANT · ROOTS / STEM / LEAVES", new Vector3(0f, 2.30f, 0f));
    }

    private static void BuildAnimalAnatomy(Vector3 basePosition)
    {
        Transform exhibit = CreateExhibit("Mammal Anatomy Model", "Animal Anatomy", "Biology model",
            "This representative mammal model shows how a skeleton supports the body, muscles move it, and the lungs, heart, and digestive organs work together. Animals vary enormously, so this is one teaching example rather than a model of every animal.",
            "animal, mammal, biology, skeleton, lungs, heart, anatomy, learning-model", basePosition, new Color(0.81f, 0.46f, 0.28f));
        Material fur = MakeMaterial("Mammal warm coat", new Color(0.57f, 0.34f, 0.19f));
        GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        torso.name = "Mammal torso"; torso.transform.SetParent(exhibit, false); torso.transform.localPosition = new Vector3(0f, 1.15f, 0.05f); torso.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); torso.transform.localScale = new Vector3(0.82f, 0.66f, 0.72f); torso.GetComponent<Renderer>().sharedMaterial = fur; Object.Destroy(torso.GetComponent<Collider>());
        MakeSphere(exhibit, "Mammal head", new Vector3(0f, 1.50f, -0.79f), new Vector3(0.65f, 0.55f, 0.55f), fur);
        MakeSphere(exhibit, "Mammal left ear", new Vector3(-0.25f, 1.86f, -0.92f), new Vector3(0.17f, 0.35f, 0.12f), fur);
        MakeSphere(exhibit, "Mammal right ear", new Vector3(0.25f, 1.86f, -0.92f), new Vector3(0.17f, 0.35f, 0.12f), fur);
        Material dark = MakeMaterial("Mammal eyes", new Color(0.04f, 0.055f, 0.07f));
        MakeSphere(exhibit, "Mammal eye", new Vector3(-0.23f, 1.57f, -1.02f), Vector3.one * 0.09f, dark);
        MakeSphere(exhibit, "Mammal eye", new Vector3(0.23f, 1.57f, -1.02f), Vector3.one * 0.09f, dark);

        for (int side = -1; side <= 1; side += 2)
            foreach (float z in new[] { -0.40f, 0.52f })
                AddBeam(exhibit, "Mammal leg", new Vector3(side * 0.27f, 0.84f, z), new Vector3(side * 0.30f, 0.20f, z - 0.08f), 0.15f, fur);
        AddBeam(exhibit, "Mammal tail", new Vector3(0.08f, 1.2f, 0.82f), new Vector3(0.30f, 1.55f, 1.42f), 0.07f, fur);

        Material bone = MakeMaterial("Mammal skeleton", new Color(0.91f, 0.85f, 0.70f));
        Material lung = MakeMaterial("Mammal lungs", new Color(0.28f, 0.61f, 0.86f));
        Material heart = MakeMaterial("Mammal heart", new Color(0.86f, 0.19f, 0.24f), true);
        AddBeam(exhibit, "Simplified spine", new Vector3(0f, 1.26f, -0.60f), new Vector3(0f, 1.26f, 0.65f), 0.045f, bone);
        for (int rib = 0; rib < 5; rib++)
        {
            float z = -0.30f + rib * 0.22f;
            AddBeam(exhibit, "Simplified rib", new Vector3(0f, 1.47f, z), new Vector3(-0.30f, 1.00f, z), 0.035f, bone);
            AddBeam(exhibit, "Simplified rib", new Vector3(0f, 1.47f, z), new Vector3(0.30f, 1.00f, z), 0.035f, bone);
        }
        MakeSphere(exhibit, "Left lung", new Vector3(-0.18f, 1.32f, 0.02f), new Vector3(0.23f, 0.30f, 0.29f), lung);
        MakeSphere(exhibit, "Right lung", new Vector3(0.18f, 1.32f, 0.02f), new Vector3(0.23f, 0.30f, 0.29f), lung);
        MakeSphere(exhibit, "Heart", new Vector3(0f, 1.16f, -0.20f), new Vector3(0.21f, 0.26f, 0.22f), heart);
        AddLabel(exhibit, "MAMMAL · SKELETON / LUNGS / HEART", new Vector3(0f, 2.42f, 0f));
    }

    private static void BuildSunflower(Vector3 basePosition)
    {
        Transform exhibit = CreateExhibit("Sunflower Anatomy Model", "Sunflower", "Plant species model",
            "A sunflower is an annual flowering plant. Broad leaves capture light, roots absorb water and minerals, and the flower head contains many small florets that can form seeds. The model is a simplified botanical diagram.",
            "plant, sunflower, botany, flower, seeds, photosynthesis, learning-model", basePosition, new Color(0.97f, 0.72f, 0.20f));
        Material stem = MakeMaterial("Sunflower stem", new Color(0.26f, 0.56f, 0.28f));
        Material petal = MakeMaterial("Sunflower petals", new Color(1f, 0.73f, 0.12f), true);
        Material center = MakeMaterial("Sunflower seed head", new Color(0.34f, 0.19f, 0.08f));
        AddBeam(exhibit, "Sunflower stem", new Vector3(0f, 0.38f, 0f), new Vector3(0f, 1.75f, 0f), 0.10f, stem);
        MakeSphere(exhibit, "Sunflower leaf", new Vector3(-0.32f, 1.04f, 0f), new Vector3(0.62f, 0.24f, 0.12f), stem);
        MakeSphere(exhibit, "Sunflower leaf", new Vector3(0.35f, 1.40f, 0f), new Vector3(0.62f, 0.24f, 0.12f), stem);
        Vector3 head = new Vector3(0f, 2.03f, 0f);
        for (int i = 0; i < 12; i++)
        {
            float angle = i * Mathf.PI * 2f / 12f;
            MakeSphere(exhibit, "Sunflower petal", head + new Vector3(Mathf.Cos(angle) * 0.34f, Mathf.Sin(angle) * 0.34f, 0f), new Vector3(0.25f, 0.14f, 0.08f), petal);
        }
        MakeSphere(exhibit, "Sunflower seed disk", head, new Vector3(0.42f, 0.42f, 0.16f), center);
        AddLabel(exhibit, "SUNFLOWER · FLORETS / SEEDS / LEAVES", new Vector3(0f, 2.55f, 0f));
    }

    private static void BuildOakTree(Vector3 basePosition)
    {
        Transform exhibit = CreateExhibit("Oak Tree Species Model", "Oak Tree", "Plant species model",
            "An oak is a long-lived tree with lobed leaves and acorns. Its woody trunk transports water and sugars, while its canopy captures light; acorns and leaves support many other species. This model is a simplified species study.",
            "plant, oak tree, acorn, tree, botany, ecology, learning-model", basePosition, new Color(0.30f, 0.62f, 0.34f));
        Material trunk = MakeMaterial("Oak bark", new Color(0.37f, 0.24f, 0.15f));
        Material canopy = MakeMaterial("Oak foliage", new Color(0.19f, 0.50f, 0.24f));
        Material acorn = MakeMaterial("Oak acorns", new Color(0.56f, 0.34f, 0.16f));
        AddBeam(exhibit, "Oak trunk", new Vector3(0f, 0.38f, 0f), new Vector3(0f, 1.8f, 0f), 0.24f, trunk);
        AddBeam(exhibit, "Oak branch", new Vector3(0f, 1.34f, 0f), new Vector3(-0.62f, 2.05f, 0f), 0.12f, trunk);
        AddBeam(exhibit, "Oak branch", new Vector3(0f, 1.48f, 0f), new Vector3(0.64f, 2.08f, 0f), 0.12f, trunk);
        MakeSphere(exhibit, "Oak canopy", new Vector3(0f, 2.18f, 0f), new Vector3(1.4f, 0.88f, 0.85f), canopy);
        MakeSphere(exhibit, "Oak canopy", new Vector3(-0.58f, 2.04f, 0.08f), new Vector3(0.86f, 0.67f, 0.70f), canopy);
        MakeSphere(exhibit, "Oak canopy", new Vector3(0.58f, 2.02f, -0.08f), new Vector3(0.86f, 0.67f, 0.70f), canopy);
        MakeSphere(exhibit, "Acorn", new Vector3(-0.35f, 1.78f, -0.32f), new Vector3(0.17f, 0.23f, 0.16f), acorn);
        MakeSphere(exhibit, "Acorn", new Vector3(0.38f, 1.77f, -0.30f), new Vector3(0.17f, 0.23f, 0.16f), acorn);
        AddLabel(exhibit, "OAK TREE · TRUNK / CANOPY / ACORNS", new Vector3(0f, 2.85f, 0f));
    }

    private static void BuildLightSpectrum(Vector3 basePosition)
    {
        Transform exhibit = CreateExhibit("Light Spectrum Model", "Light Spectrum", "Physics model",
            "Visible light is a continuous band of wavelengths, from longer red wavelengths to shorter violet ones. The visible band is only a small part of the electromagnetic spectrum.",
            "physics, light, color, spectrum, wavelength, learning-model", basePosition, violet.color);
        Color[] spectrum = { new Color(0.88f, 0.16f, 0.18f), new Color(0.95f, 0.43f, 0.12f), new Color(0.96f, 0.80f, 0.15f), new Color(0.26f, 0.67f, 0.33f), new Color(0.15f, 0.57f, 0.85f), new Color(0.26f, 0.36f, 0.78f), new Color(0.58f, 0.28f, 0.75f) };
        for (int i = 0; i < spectrum.Length; i++)
        {
            GameObject band = GameObject.CreatePrimitive(PrimitiveType.Cube);
            band.name = "Visible color band"; band.transform.SetParent(exhibit, false); band.transform.localPosition = new Vector3((i - 3) * 0.30f, 1.34f, 0f); band.transform.localScale = new Vector3(0.31f, 1.05f, 0.24f); band.GetComponent<Renderer>().sharedMaterial = MakeMaterial("Spectrum " + i, spectrum[i]); Object.Destroy(band.GetComponent<Collider>());
        }
        AddLabel(exhibit, "VISIBLE LIGHT · RED → VIOLET", new Vector3(0f, 2.08f, 0f));
    }

    private static void BuildPeriodicTable(Vector3 basePosition)
    {
        Transform exhibit = CreateExhibit("Periodic Table Model", "Periodic Table", "Chemistry model",
            "The periodic table orders elements by atomic number. Its rows are periods and columns are groups; elements in a group often have similar chemical behavior. The compact display is an overview, not a substitute for detailed element data.",
            "chemistry, periodic-table, elements, learning-model", basePosition, new Color(0.39f, 0.61f, 0.83f));
        string[] symbols = { "H","He","Li","Be","B","C","N","O","F","Ne","Na","Mg","Al","Si","P","S","Cl","Ar","K","Ca","Fe","Cu","Zn","Ag","Au","Hg","Pb","U" };
        Vector3 origin = new Vector3(-1.48f, 1.0f, 0f);
        for (int i = 0; i < symbols.Length; i++)
        {
            int row, col;
            if (i == 0) { row = 0; col = 0; }
            else if (i == 1) { row = 0; col = 13; }
            else { row = 1 + (i - 2) / 14; col = (i - 2) % 14; }
            GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tile.name = "Element " + symbols[i]; tile.transform.SetParent(exhibit, false); tile.transform.localPosition = origin + new Vector3(col * 0.22f, row * 0.20f, 0f); tile.transform.localScale = new Vector3(0.19f, 0.17f, 0.08f);
            Color elementColor = i % 4 == 0 ? cyan.color : i % 4 == 1 ? green.color : i % 4 == 2 ? gold.color : violet.color;
            tile.GetComponent<Renderer>().sharedMaterial = MakeMaterial("Element " + symbols[i], elementColor); Object.Destroy(tile.GetComponent<Collider>());
            AddTinyLabel(tile.transform, symbols[i]);
        }
        AddLabel(exhibit, "ELEMENTS · ATOMIC NUMBER ORDERS THE TABLE", new Vector3(0f, 2.14f, 0f));
    }

    private static void AddTinyLabel(Transform parent, string value)
    {
        GameObject go = new GameObject("ElementSymbol", typeof(TextMeshPro));
        go.transform.SetParent(parent, false); go.transform.localPosition = new Vector3(0f, 0f, -0.55f); go.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        TextMeshPro text = go.GetComponent<TextMeshPro>(); text.text = value; text.fontSize = 0.28f; text.fontStyle = FontStyles.Bold; text.color = Color.white; text.alignment = TextAlignmentOptions.Center; text.rectTransform.sizeDelta = new Vector2(0.22f, 0.16f); go.AddComponent<WorldLabelBillboard>();
    }

    private static void AddBeam(Transform parent, string name, Vector3 from, Vector3 to, float width, Material material)
    {
        GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        beam.name = name; beam.transform.SetParent(parent, false); beam.transform.localPosition = (from + to) * 0.5f;
        Vector3 difference = to - from; beam.transform.localRotation = Quaternion.FromToRotation(Vector3.up, difference.normalized);
        beam.transform.localScale = new Vector3(width, difference.magnitude * 0.5f, width);
        beam.GetComponent<Renderer>().sharedMaterial = material; Object.Destroy(beam.GetComponent<Collider>());
    }

    private static Transform CreateExhibit(string rootName, string displayName, string category, string description, string tags, Vector3 floorPosition, Color accent)
    {
        GameObject pedestal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pedestal.name = rootName + " Pedestal";
        pedestal.transform.SetParent(root, false);
        pedestal.transform.position = floorPosition + Vector3.up * 0.40f;
        pedestal.transform.localScale = new Vector3(2.2f, 0.4f, 2.2f);
        pedestal.GetComponent<Renderer>().sharedMaterial = deepBlue;
        pedestal.GetComponent<Collider>().enabled=false;
        Object.Destroy(pedestal.GetComponent<Collider>());
        var solid=pedestal.AddComponent<BoxCollider>();solid.size=new Vector3(.9f,2,.9f);
        var obstacle=pedestal.AddComponent<UnityEngine.AI.NavMeshObstacle>();obstacle.carving=true;obstacle.shape=UnityEngine.AI.NavMeshObstacleShape.Box;obstacle.size=solid.size;

        GameObject exhibit = new GameObject(rootName);
        exhibit.transform.SetParent(root, false);
        exhibit.transform.position = floorPosition;
        ObjectMetadata metadata = exhibit.AddComponent<ObjectMetadata>();
        metadata.objectId = "classroom_model_" + displayName.ToLowerInvariant().Replace(' ', '_');
        metadata.displayName = displayName;
        metadata.objectName = displayName;
        metadata.category = category;
        metadata.descriptionSummary = description;
        metadata.description = description;
        ActiveDescription=description;
        metadata.contextualTags = tags;
        metadata.scenario = "classroom";
        metadata.isInteractable = true;
        metadata.interactionHint = "Press E to ask for an explanation or quiz";
        SphereCollider selection = exhibit.AddComponent<SphereCollider>();
        selection.center = new Vector3(0f, 1.25f, 0f);
        selection.radius = 1.15f;
        selection.isTrigger = true;
        GameObject keyLightObject = new GameObject("Learning exhibit soft key light");
        keyLightObject.transform.SetParent(exhibit.transform, false);
        Vector3 toCamera = Camera.main != null ? Camera.main.transform.position - floorPosition : Vector3.back;
        toCamera.y = 0f;
        if (toCamera.sqrMagnitude < 0.01f) toCamera = Vector3.back;
        keyLightObject.transform.localPosition = toCamera.normalized * 1.4f + Vector3.up * 3.2f;
        Light keyLight = keyLightObject.AddComponent<Light>();
        keyLight.type = LightType.Point;
        keyLight.color = new Color(1f, 0.91f, 0.77f);
        keyLight.intensity = 2.6f;
        keyLight.range = 9f;
        keyLight.shadows = LightShadows.None;
        AddOrbitLine(exhibit.transform, new Vector3(0f, 0.64f, 0f), 0.72f, Vector3.right, Vector3.forward, MakeMaterial(rootName + " Accent", accent), "Display plinth trim");
        return exhibit.transform;
    }

    private static SphereCollider MakeSphere(Transform parent, string name, Vector3 localPosition, Vector3 scale, Material material)
    {
        GameObject item = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        item.name = name;
        item.transform.SetParent(parent, false);
        item.transform.localPosition = localPosition;
        item.transform.localScale = scale;
        Renderer renderer = item.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;
        return item.GetComponent<SphereCollider>();
    }

    private static void AddOrbitLine(Transform parent, Vector3 center, float radius, Vector3 axisA, Vector3 axisB, Material material, string name)
    {
        GameObject lineObject = new GameObject(name);
        lineObject.transform.SetParent(parent, false);
        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = 80;
        line.startWidth = line.endWidth = 0.018f;
        line.sharedMaterial = material;
        line.startColor = line.endColor = material.color;
        line.numCapVertices = 3;
        Vector3 a = axisA.normalized;
        Vector3 b = axisB.normalized;
        for (int i = 0; i < 80; i++)
        {
            float t = i / 79f * Mathf.PI * 2f;
            line.SetPosition(i, center + (a * Mathf.Cos(t) + b * Mathf.Sin(t)) * radius);
        }
    }

    private static void AddLabel(Transform parent, string label, Vector3 localPosition)
    {
        GameObject go = new GameObject("ExhibitLabel", typeof(RectTransform), typeof(TextMeshPro));
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        TextMeshPro text = go.GetComponent<TextMeshPro>();
        text.text = label;
        text.fontSize = 0.95f;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.outlineWidth = 0.10f;
        text.outlineColor = new Color(0.03f, 0.06f, 0.10f);
        text.rectTransform.sizeDelta = new Vector2(3.6f, 0.36f);
        go.AddComponent<WorldLabelBillboard>();
    }

    private static Transform FindTransformContains(string name)
    {
        GameObject exact = GameObject.Find(name);
        if (exact != null) return exact.transform;
        foreach (Transform item in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
            if (item.name.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0) return item;
        return null;
    }

    private static void MakeMaterials()
    {
        deepBlue = MakeMaterial("Exhibit Navy", new Color(0.07f, 0.12f, 0.19f));
        cyan = MakeMaterial("Exhibit Cyan", new Color(0.24f, 0.78f, 0.88f));
        coral = MakeMaterial("Exhibit Coral", new Color(0.91f, 0.30f, 0.27f));
        gold = MakeMaterial("Exhibit Gold", new Color(1f, 0.70f, 0.20f), true);
        white = MakeMaterial("Exhibit Ivory", new Color(0.91f, 0.90f, 0.82f));
        green = MakeMaterial("Exhibit Green", new Color(0.24f, 0.69f, 0.39f));
        violet = MakeMaterial("Exhibit Violet", new Color(0.52f, 0.45f, 0.91f));
        cellWall = MakeMaterial("Exhibit Cell Wall", new Color(0.32f, 0.52f, 0.30f));
    }

    private static Material MakeMaterial(string name, Color color, bool emissive = false)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Unlit/Color");
        Material material = LumenRuntimeResources.Own(new Material(shader) { name = name, color = color });
        if (emissive && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 0.6f);
        }
        return material;
    }
}

public sealed class LearningModelSpin : MonoBehaviour
{
    private void Update()
    {
        if(!LumenAccessibility.ReducedMotion&&LearningModelInspection.Current?.IsInspecting!=true)transform.Rotate(Vector3.up, 12f * Time.deltaTime, Space.World);
    }
}

