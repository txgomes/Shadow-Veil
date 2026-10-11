using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Runtime-built vertical battlefield for the first native Unity slice.</summary>
public sealed class ShadowVeilGame : MonoBehaviour
{
    private const float LaneWidth = 8.8f;
    private const float TileLength = 18f;
    private const int TileCount = 9;
    private readonly List<GameObject> floorTiles = new List<GameObject>();
    private readonly List<Unit> allies = new List<Unit>();
    private readonly List<Unit> enemies = new List<Unit>();
    private readonly List<Material> materials = new List<Material>();
    private readonly List<Button> cardButtons = new List<Button>();
    private readonly List<int> cardCosts = new List<int>();
    private readonly List<Projectile> projectiles = new List<Projectile>();
    private readonly List<VisualEffect> visualEffects = new List<VisualEffect>();
    private readonly Dictionary<Collider, Unit> unitsByCollider = new Dictionary<Collider, Unit>();
    private readonly Dictionary<Collider, float> manaPickups = new Dictionary<Collider, float>();

    private Unit hero;
    private Unit selectedEnemy;
    private Camera battleCamera;
    private Canvas canvas;
    private Mesh pineConeMesh;
    private Material arrowMaterial;
    private Material arrowTipMaterial;
    private Text healthText;
    private Text manaText;
    private Text objectiveText;
    private Text timerText;
    private Image healthFill;
    private Image manaFill;
    private float mana = 12f;
    private float timeLeft = 210f;
    private float spawnClock = 1.2f;
    private float pickupClock = 4f;
    private float walkPhase;
    private int kills;
    private int wave = 1;
    private bool dragging;
    private Vector3 destination;
    private bool hasDestination;
    private bool ended;

    private sealed class Projectile
    {
        public GameObject root;
        public Unit target;
        public float damage;
        public float speed;
    }

    private sealed class VisualEffect
    {
        public GameObject root;
        public Material material;
        public Color color;
        public Vector3 endScale;
        public float lifetime;
        public float age;
    }

    private sealed class Unit
    {
        public GameObject root;
        public Transform torso;
        public Transform upperBody;
        public Transform leftLeg;
        public Transform rightLeg;
        public Transform leftArm;
        public Transform rightArm;
        public GameObject selectionDisc;
        public Image healthFill;
        public bool isEnemy;
        public bool isHero;
        public string role;
        public float hp;
        public float maxHp;
        public float speed;
        public float range;
        public float damage;
        public float cooldown;
        public float attackRate;
        public float healCooldown;
        public float attackTime;
        public float phase;
        public float visualScale;
    }

    private void Start()
    {
        Application.targetFrameRate = 60;
        BuildWorld();
        hero = CreateUnit("Ashen Ranger", false, true, "ranger", new Color(0.68f, 0.34f, 0.29f), new Color(0.95f, 0.70f, 0.56f), Vector3.zero);
        hero.hp = hero.maxHp = 100f;
        hero.speed = 6f;
        destination = hero.root.transform.position;
        BuildCamera();
        BuildHud();
    }

    private void Update()
    {
        if (ended) return;
        float dt = Time.deltaTime;
        timeLeft -= dt;
        mana = Mathf.Min(26f, mana + 0.65f * dt);
        ReadMovementInput();
        MoveHero(dt);
        UpdateCamera(dt);
        UpdateFloor();
        UpdateUnits(dt);
        HeroAttack(dt);
        UpdateProjectiles(dt);
        UpdateVisualEffects(dt);
        SpawnEnemies(dt);
        SpawnManaPickups(dt);
        UpdateHud();
        if (hero.hp <= 0f || timeLeft <= 0f) EndGame(false);
    }

    private void BuildWorld()
    {
        RenderSettings.ambientLight = new Color(0.24f, 0.27f, 0.34f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.075f, 0.095f, 0.12f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 36f;
        RenderSettings.fogEndDistance = 92f;

        GameObject moon = new GameObject("Moonlight");
        Light sun = moon.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(0.63f, 0.72f, 0.92f);
        sun.intensity = 1.35f;
        sun.shadows = LightShadows.Soft;
        moon.transform.rotation = Quaternion.Euler(38f, -32f, 0f);

        Material earth = MakeMaterial(new Color(0.42f, 0.37f, 0.32f));
        earth.mainTexture = MakeGroundTexture();
        earth.mainTextureScale = new Vector2(1f, 2f);
        Material edge = MakeMaterial(new Color(0.07f, 0.10f, 0.105f));
        Material stone = MakeMaterial(new Color(0.24f, 0.26f, 0.30f));
        Material bark = MakeMaterial(new Color(0.19f, 0.12f, 0.085f));
        Material pine = MakeMaterial(new Color(0.075f, 0.16f, 0.13f));
        Material pineLight = MakeMaterial(new Color(0.10f, 0.22f, 0.17f));
        arrowMaterial = MakeMetal(new Color(0.62f, 0.50f, 0.33f), 0.4f);
        arrowTipMaterial = MakeGlow(new Color(0.92f, 0.52f, 0.19f), 1.3f);
        pineConeMesh = CreateConeMesh();

        MakePrimitive("Forest floor", PrimitiveType.Cube, new Vector3(0f, -0.86f, 54f), new Vector3(180f, 0.3f, 300f), edge);
        for (int i = 0; i < TileCount; i++)
        {
            GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tile.name = "Trail tile " + i;
            tile.transform.position = new Vector3(0f, -0.56f, (i - 2) * TileLength);
            tile.transform.localScale = new Vector3(LaneWidth, 0.55f, TileLength);
            tile.GetComponent<Renderer>().sharedMaterial = earth;
            floorTiles.Add(tile);
        }

        MakePrimitive("Left trail edge", PrimitiveType.Cube, new Vector3(-7.55f, -0.28f, 45f), new Vector3(0.34f, 0.32f, 145f), stone);
        MakePrimitive("Right trail edge", PrimitiveType.Cube, new Vector3(7.55f, -0.28f, 45f), new Vector3(0.34f, 0.32f, 145f), stone);
        for (int i = 0; i < 34; i++)
        {
            float z = -10f + i * 4.7f;
            float side = (i % 2 == 0) ? -1f : 1f;
            float x = side * Random.Range(5.1f, 10.5f);
            float height = Random.Range(2.5f, 4.1f);
            MakePrimitive("Pine trunk", PrimitiveType.Cylinder, new Vector3(x, height * 0.48f, z), new Vector3(0.18f, height * 0.48f, 0.18f), bark);
            MakePineCrown("Pine crown lower", new Vector3(x, height, z), 2.65f, 2.65f, pine);
            MakePineCrown("Pine crown upper", new Vector3(x, height + 0.95f, z), 1.85f, 2.25f, pineLight);
            for (int rock = 0; rock < 2; rock++)
            {
                float rockZ = z + Random.Range(-1.7f, 1.7f);
                float rockX = side * Random.Range(4.55f, 5.6f);
                GameObject stoneObj = MakePrimitive("Trail stone", PrimitiveType.Sphere, new Vector3(rockX, -0.12f, rockZ), new Vector3(Random.Range(0.35f, 0.8f), Random.Range(0.2f, 0.55f), Random.Range(0.35f, 0.75f)), stone);
                stoneObj.transform.rotation = Quaternion.Euler(Random.Range(-12f, 12f), Random.Range(0f, 360f), Random.Range(-12f, 12f));
            }
        }

        // Sparse firefly lights give the forest depth without adding a heavy post-processing dependency.
        for (int i = 0; i < 5; i++)
        {
            float z = 8f + i * 23f;
            float x = i % 2 == 0 ? -4.05f : 4.05f;
            MakePrimitive("Veil lantern", PrimitiveType.Sphere, new Vector3(x, 2.0f, z), new Vector3(0.24f, 0.32f, 0.24f), MakeGlow(new Color(1f, 0.48f, 0.18f), 2.2f));
            GameObject glow = new GameObject("Lantern glow");
            glow.transform.position = new Vector3(x, 2f, z);
            Light light = glow.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.43f, 0.16f);
            light.intensity = 2.2f;
            light.range = 7f;
        }
    }

    private void BuildCamera()
    {
        GameObject cameraObject = new GameObject("Trail camera");
        battleCamera = cameraObject.AddComponent<Camera>();
        battleCamera.orthographic = true;
        battleCamera.orthographicSize = 9.5f;
        battleCamera.fieldOfView = 42f;
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 13f, -10f);
        cameraObject.transform.rotation = Quaternion.Euler(52f, 0f, 0f);
    }

    private void BuildHud()
    {
        GameObject canvasObject = new GameObject("Battle HUD");
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(390f, 844f);
        scaler.matchWidthOrHeight = 0.45f;
        canvasObject.AddComponent<GraphicRaycaster>();
        if (EventSystem.current == null)
        {
            GameObject events = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            events.name = "EventSystem";
        }

        Panel("Top HUD", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(-16f, -148f), new Vector2(-16f, -8f), new Color(0.05f, 0.045f, 0.055f, 0.82f));
        AddText("brand", "SHADOW VEIL", 18, TextAnchor.MiddleLeft, new Vector2(0.04f, 0.953f), new Vector2(0.66f, 0.99f), new Color(0.87f, 0.72f, 0.48f));
        timerText = AddText("timer", "03:30", 16, TextAnchor.MiddleRight, new Vector2(0.68f, 0.953f), new Vector2(0.96f, 0.99f), Color.white);
        healthText = AddText("health", "HERO  100 / 100", 12, TextAnchor.MiddleLeft, new Vector2(0.04f, 0.905f), new Vector2(0.48f, 0.94f), Color.white);
        manaText = AddText("mana", "MANA  12 / 26", 12, TextAnchor.MiddleLeft, new Vector2(0.53f, 0.905f), new Vector2(0.96f, 0.94f), new Color(0.87f, 0.67f, 1f));
        healthFill = AddBar("Health fill", new Vector2(0.04f, 0.89f), new Vector2(0.48f, 0.901f), new Color(0.77f, 0.22f, 0.30f));
        manaFill = AddBar("Mana fill", new Vector2(0.53f, 0.89f), new Vector2(0.96f, 0.901f), new Color(0.62f, 0.20f, 0.80f));
        objectiveText = AddText("objective", "WRAITHS: 0 / 15   ·   ALLIES: 0", 12, TextAnchor.MiddleCenter, new Vector2(0.08f, 0.861f), new Vector2(0.92f, 0.882f), Color.white);
        AddText("controls", "DRAG TO MOVE   ·   TAP ENEMIES TO TARGET   ·   COLLECT URNS", 9, TextAnchor.MiddleCenter, new Vector2(0.04f, 0.835f), new Vector2(0.96f, 0.854f), new Color(0.77f, 0.72f, 0.63f));

        Panel("Card dock", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(-10f, 8f), new Vector2(10f, 118f), new Color(0.045f, 0.04f, 0.055f, 0.95f));
        AddCard("GRAVEGUARD", "MELEE", 5, 0, new Color(0.46f, 0.43f, 0.48f), () => Deploy("tank"));
        AddCard("SKELETON ARCHER", "RANGED", 5, 1, new Color(0.34f, 0.48f, 0.43f), () => Deploy("ranged"));
        AddCard("TROLL HEALER", "SUPPORT", 5, 2, new Color(0.32f, 0.50f, 0.40f), () => Deploy("healer"));
        AddCard("GLOAM IMP", "ASSAULT", 9, 3, new Color(0.58f, 0.34f, 0.30f), () => Deploy("assault"));
        AddCard("CINDERBURST", "SPELL", 2, 4, new Color(0.63f, 0.30f, 0.22f), CastCinderburst);
        AddCard("RIME VEIL", "HEAL", 5, 5, new Color(0.30f, 0.46f, 0.55f), CastRimeVeil);
    }

    private void ReadMovementInput()
    {
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Began)
                {
                    bool overHud = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId);
                    dragging = !overHud && !HandleWorldTap(touch.position);
                    if (dragging) SetDestination(touch.position);
                }
                if (dragging && (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary))
                    SetDestination(touch.position);
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) dragging = false;
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                bool overHud = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
                dragging = !overHud && !HandleWorldTap(Input.mousePosition);
                if (dragging) SetDestination(Input.mousePosition);
            }
            if (dragging && (Input.GetMouseButton(0) || Input.GetMouseButtonDown(0))) SetDestination(Input.mousePosition);
            if (Input.GetMouseButtonUp(0)) dragging = false;
        }
    }

    private void SetDestination(Vector2 screenPosition)
    {
        Ray ray = battleCamera.ScreenPointToRay(screenPosition);
        Plane ground = new Plane(Vector3.up, Vector3.zero);
        if (ground.Raycast(ray, out float distance))
        {
            Vector3 point = ray.GetPoint(distance);
            destination = new Vector3(Mathf.Clamp(point.x, -LaneWidth * 0.43f, LaneWidth * 0.43f), 0f, Mathf.Max(hero.root.transform.position.z - 3f, point.z));
            hasDestination = true;
        }
    }

    private void MoveHero(float dt)
    {
        if (!hasDestination) { Animate(hero, dt, false); return; }
        Vector3 delta = destination - hero.root.transform.position;
        delta.y = 0f;
        if (delta.magnitude < 0.16f) { hasDestination = false; Animate(hero, dt, false); return; }
        Vector3 direction = delta.normalized;
        hero.root.transform.position += direction * hero.speed * dt;
        hero.root.transform.rotation = Quaternion.RotateTowards(hero.root.transform.rotation, Quaternion.LookRotation(direction), 540f * dt);
        Animate(hero, dt, true);
    }

    private void UpdateCamera(float dt)
    {
        Vector3 desired = hero.root.transform.position + new Vector3(0f, 13f, -10f);
        battleCamera.transform.position = Vector3.Lerp(battleCamera.transform.position, desired, 1f - Mathf.Exp(-4.5f * dt));
        battleCamera.transform.rotation = Quaternion.Euler(52f, 0f, 0f);
    }

    private void UpdateFloor()
    {
        float furthestZ = float.MinValue;
        foreach (GameObject tile in floorTiles) furthestZ = Mathf.Max(furthestZ, tile.transform.position.z);
        foreach (GameObject tile in floorTiles)
        {
            if (tile.transform.position.z + TileLength * 0.5f < hero.root.transform.position.z - TileLength)
            {
                furthestZ += TileLength;
                tile.transform.position = new Vector3(0f, -0.56f, furthestZ);
            }
        }
    }

    private void SpawnEnemies(float dt)
    {
        spawnClock -= dt;
        if (spawnClock > 0f || enemies.Count >= 5 || kills >= 15) return;
        float x = Random.Range(-3.1f, 3.1f);
        bool elite = Random.value < 0.20f;
        Unit enemy = CreateUnit(elite ? "Elite Skeleton" : "Skeleton Warrior", true, false, "enemy", new Color(0.37f, 0.32f, 0.43f), new Color(0.77f, 0.72f, 0.65f), new Vector3(x, 0f, hero.root.transform.position.z + Random.Range(17f, 26f)));
        enemy.hp = enemy.maxHp = elite ? 78f : 42f;
        enemy.speed = elite ? 2.4f : 3.2f;
        enemy.damage = elite ? 12f : 7f;
        enemy.attackRate = 1.1f;
        enemy.range = 1.7f;
        enemies.Add(enemy);
        spawnClock = Mathf.Max(1.2f, 3.2f - wave * 0.35f);
    }

    private bool HandleWorldTap(Vector2 screenPosition)
    {
        Ray ray = battleCamera.ScreenPointToRay(screenPosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f);
        float nearest = float.MaxValue;
        Collider tapped = null;
        foreach (RaycastHit hit in hits)
        {
            if (hit.distance < nearest && (unitsByCollider.ContainsKey(hit.collider) || manaPickups.ContainsKey(hit.collider)))
            {
                nearest = hit.distance;
                tapped = hit.collider;
            }
        }
        if (tapped == null) return false;

        if (manaPickups.TryGetValue(tapped, out float manaValue))
        {
            manaPickups.Remove(tapped);
            mana = Mathf.Min(26f, mana + manaValue);
            Destroy(tapped.gameObject);
            return true;
        }

        if (unitsByCollider.TryGetValue(tapped, out Unit unit) && unit.isEnemy)
        {
            if (selectedEnemy != null && selectedEnemy.selectionDisc != null) selectedEnemy.selectionDisc.SetActive(false);
            selectedEnemy = unit;
            if (selectedEnemy.selectionDisc != null) selectedEnemy.selectionDisc.SetActive(true);
            return true;
        }
        return false;
    }

    private void SpawnManaPickups(float dt)
    {
        List<Collider> expired = new List<Collider>();
        foreach (KeyValuePair<Collider, float> pickup in manaPickups)
            if (pickup.Key == null || pickup.Key.transform.position.z < hero.root.transform.position.z - 4f) expired.Add(pickup.Key);
        foreach (Collider collider in expired)
        {
            manaPickups.Remove(collider);
            if (collider != null) Destroy(collider.gameObject);
        }

        pickupClock -= dt;
        if (pickupClock > 0f || manaPickups.Count >= 4) return;
        float x = Random.Range(-3.2f, 3.2f);
        float z = hero.root.transform.position.z + Random.Range(16f, 24f);
        GameObject jar = new GameObject("Mana urn");
        jar.transform.position = new Vector3(x, 0f, z);
        MakePrimitive("Urn body", PrimitiveType.Sphere, jar.transform.position + Vector3.up * 0.52f, new Vector3(0.66f, 0.78f, 0.66f), MakeMaterial(new Color(0.35f, 0.20f, 0.38f)), jar.transform);
        MakePrimitive("Urn neck", PrimitiveType.Cylinder, jar.transform.position + Vector3.up * 1.08f, new Vector3(0.22f, 0.18f, 0.22f), MakeMetal(new Color(0.54f, 0.40f, 0.28f), 0.5f), jar.transform);
        MakePrimitive("Mana glow", PrimitiveType.Sphere, jar.transform.position + Vector3.up * 0.75f + Vector3.back * 0.17f, new Vector3(0.22f, 0.24f, 0.12f), MakeGlow(new Color(0.48f, 0.18f, 0.95f), 2f), jar.transform);
        BoxCollider pickupCollider = jar.AddComponent<BoxCollider>();
        pickupCollider.center = new Vector3(0f, 0.65f, 0f);
        pickupCollider.size = new Vector3(0.9f, 1.4f, 0.9f);
        manaPickups[pickupCollider] = Random.Range(3f, 5f);
        pickupClock = Random.Range(8f, 12f);
    }

    private void UpdateUnits(float dt)
    {
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            Unit enemy = enemies[i];
            Unit target = FindClosestTarget(enemy.root.transform.position);
            if (target == null) continue;
            Engage(enemy, target, dt);
            if (enemy.hp <= 0f)
            {
                enemies.RemoveAt(i);
                Collider collider = enemy.root.GetComponent<Collider>();
                if (collider != null) unitsByCollider.Remove(collider);
                if (selectedEnemy == enemy) selectedEnemy = null;
                Destroy(enemy.root);
                kills++;
                mana = Mathf.Min(26f, mana + 4f);
                wave = 1 + kills / 5;
                if (kills >= 15 && enemies.Count == 0) EndGame(true);
            }
        }

        for (int i = allies.Count - 1; i >= 0; i--)
        {
            Unit ally = allies[i];
            if (ally.role == "healer") SupportAllies(ally, dt);
            else
            {
                Unit target = FindClosestEnemy(ally.root.transform.position);
                if (target != null) Engage(ally, target, dt);
                else Animate(ally, dt, false);
            }
            if (ally.hp <= 0f)
            {
                allies.RemoveAt(i);
                Collider collider = ally.root.GetComponent<Collider>();
                if (collider != null) unitsByCollider.Remove(collider);
                Destroy(ally.root);
            }
        }
    }

    private void HeroAttack(float dt)
    {
        if (selectedEnemy != null && (selectedEnemy.hp <= 0f || selectedEnemy.root == null))
        {
            if (selectedEnemy.selectionDisc != null) selectedEnemy.selectionDisc.SetActive(false);
            selectedEnemy = null;
        }
        Unit target = selectedEnemy != null ? selectedEnemy : FindClosestEnemy(hero.root.transform.position);
        if (target == null) return;
        Vector3 direction = target.root.transform.position - hero.root.transform.position;
        direction.y = 0f;
        if (direction.magnitude > 12f) return;
        hero.root.transform.rotation = Quaternion.RotateTowards(hero.root.transform.rotation, Quaternion.LookRotation(direction.normalized), 540f * dt);
        hero.cooldown -= dt;
        if (hero.cooldown <= 0f)
        {
            hero.cooldown = 0.8f;
            BeginAttack(hero);
            FireArrow(hero, target, 8f);
        }
    }

    private void BeginAttack(Unit attacker)
    {
        attacker.attackTime = 0.36f;
    }

    private void FireArrow(Unit attacker, Unit target, float damage)
    {
        Vector3 position = attacker.root.transform.position + Vector3.up * 1.48f + attacker.root.transform.forward * 0.4f;
        GameObject arrow = new GameObject("Arrow projectile");
        arrow.transform.position = position;
        Vector3 direction = (target.root.transform.position + Vector3.up * 0.9f - position).normalized;
        arrow.transform.rotation = Quaternion.LookRotation(direction);
        GameObject shaft = MakePrimitive("Arrow shaft", PrimitiveType.Cylinder, position, new Vector3(0.025f, 0.24f, 0.025f), arrowMaterial, arrow.transform);
        shaft.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        MakePrimitive("Arrow head", PrimitiveType.Cube, position + direction * 0.27f, new Vector3(0.07f, 0.07f, 0.14f), arrowTipMaterial, arrow.transform);
        projectiles.Add(new Projectile { root = arrow, target = target, damage = damage, speed = 17f });
    }

    private void UpdateProjectiles(float dt)
    {
        for (int i = projectiles.Count - 1; i >= 0; i--)
        {
            Projectile projectile = projectiles[i];
            if (projectile.target == null || projectile.target.root == null || projectile.target.hp <= 0f)
            {
                Destroy(projectile.root);
                projectiles.RemoveAt(i);
                continue;
            }
            Vector3 targetPosition = projectile.target.root.transform.position + Vector3.up * 0.92f;
            Vector3 delta = targetPosition - projectile.root.transform.position;
            if (delta.magnitude <= projectile.speed * dt + 0.12f)
            {
                projectile.target.hp -= projectile.damage;
                if (projectile.target.healthFill != null) projectile.target.healthFill.fillAmount = Mathf.Clamp01(projectile.target.hp / projectile.target.maxHp);
                CreateImpactEffect(targetPosition, new Color(1f, 0.62f, 0.24f), Vector3.one * 0.38f, 0.18f);
                Destroy(projectile.root);
                projectiles.RemoveAt(i);
                continue;
            }
            Vector3 direction = delta.normalized;
            projectile.root.transform.position += direction * projectile.speed * dt;
            projectile.root.transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    private void CreateImpactEffect(Vector3 position, Color color, Vector3 endScale, float lifetime)
    {
        Material material = MakeGlow(color, 2f);
        GameObject effect = MakePrimitive("Impact effect", PrimitiveType.Sphere, position, Vector3.one * 0.12f, material);
        visualEffects.Add(new VisualEffect { root = effect, material = material, color = color, endScale = endScale, lifetime = lifetime });
    }

    private void UpdateVisualEffects(float dt)
    {
        for (int i = visualEffects.Count - 1; i >= 0; i--)
        {
            VisualEffect effect = visualEffects[i];
            effect.age += dt;
            float progress = Mathf.Clamp01(effect.age / effect.lifetime);
            effect.root.transform.localScale = Vector3.Lerp(Vector3.one * 0.12f, effect.endScale, progress);
            if (effect.material.HasProperty("_EmissionColor")) effect.material.SetColor("_EmissionColor", effect.color * (2f * (1f - progress)));
            if (progress >= 1f)
            {
                materials.Remove(effect.material);
                Destroy(effect.material);
                Destroy(effect.root);
                visualEffects.RemoveAt(i);
            }
        }
    }

    private void Engage(Unit attacker, Unit target, float dt)
    {
        Vector3 delta = target.root.transform.position - attacker.root.transform.position;
        delta.y = 0f;
        float distance = delta.magnitude;
        if (distance > attacker.range)
        {
            Vector3 direction = delta.normalized;
            direction += Separation(attacker);
            direction.y = 0f;
            direction.Normalize();
            attacker.root.transform.position += direction * attacker.speed * dt;
            attacker.root.transform.rotation = Quaternion.RotateTowards(attacker.root.transform.rotation, Quaternion.LookRotation(direction), 480f * dt);
            Animate(attacker, dt, true);
            return;
        }
        attacker.root.transform.rotation = Quaternion.RotateTowards(attacker.root.transform.rotation, Quaternion.LookRotation(delta.normalized), 700f * dt);
        attacker.cooldown -= dt;
        if (attacker.cooldown <= 0f)
        {
            attacker.cooldown = attacker.attackRate;
            BeginAttack(attacker);
            if (attacker.role == "ranged") FireArrow(attacker, target, attacker.damage);
            else
            {
                target.hp -= attacker.damage;
                CreateImpactEffect(target.root.transform.position + Vector3.up * 0.85f, new Color(1f, 0.42f, 0.20f), new Vector3(0.55f, 0.55f, 0.55f), 0.22f);
            }
            if (target.healthFill != null) target.healthFill.fillAmount = Mathf.Clamp01(target.hp / target.maxHp);
        }
        Animate(attacker, dt, false);
    }

    private Vector3 Separation(Unit unit)
    {
        Vector3 push = Vector3.zero;
        List<Unit> group = unit.isEnemy ? enemies : allies;
        foreach (Unit other in group)
        {
            if (other == unit || other.root == null) continue;
            Vector3 away = unit.root.transform.position - other.root.transform.position;
            away.y = 0f;
            float distance = away.magnitude;
            if (distance > 0.01f && distance < 1.8f) push += away.normalized * ((1.8f - distance) / 1.8f);
        }
        return push * 1.4f;
    }

    private void SupportAllies(Unit healer, float dt)
    {
        Unit target = hero.hp < hero.maxHp ? hero : null;
        float lowestHealth = target != null ? hero.hp / hero.maxHp : 1f;
        foreach (Unit ally in allies)
        {
            if (ally == healer || ally.hp <= 0f) continue;
            float healthRatio = ally.hp / ally.maxHp;
            if (healthRatio < lowestHealth)
            {
                target = ally;
                lowestHealth = healthRatio;
            }
        }
        if (target == null) { Animate(healer, dt, false); return; }
        Vector3 delta = target.root.transform.position - healer.root.transform.position;
        delta.y = 0f;
        if (delta.magnitude > 2.4f)
        {
            Vector3 direction = delta.normalized + Separation(healer);
            direction.y = 0f;
            direction.Normalize();
            healer.root.transform.position += direction * healer.speed * dt;
            healer.root.transform.rotation = Quaternion.RotateTowards(healer.root.transform.rotation, Quaternion.LookRotation(direction), 480f * dt);
            Animate(healer, dt, true);
            return;
        }
        healer.root.transform.rotation = Quaternion.RotateTowards(healer.root.transform.rotation, Quaternion.LookRotation(delta.normalized), 480f * dt);
        healer.healCooldown -= dt;
        if (healer.healCooldown <= 0f)
        {
            healer.healCooldown = 1.4f;
            target.hp = Mathf.Min(target.maxHp, target.hp + 12f);
            CreateImpactEffect(target.root.transform.position + Vector3.up * 1.4f, new Color(0.26f, 0.88f, 0.61f), new Vector3(0.7f, 1.2f, 0.7f), 0.55f);
        }
        Animate(healer, dt, false);
    }

    private Unit FindClosestTarget(Vector3 from)
    {
        Unit best = hero;
        float bestDistance = Vector3.Distance(from, hero.root.transform.position);
        foreach (Unit ally in allies)
        {
            float d = Vector3.Distance(from, ally.root.transform.position);
            if (d < bestDistance) { best = ally; bestDistance = d; }
        }
        return best;
    }

    private Unit FindClosestEnemy(Vector3 from)
    {
        Unit best = null;
        float bestDistance = float.MaxValue;
        foreach (Unit enemy in enemies)
        {
            if (enemy.hp <= 0f || enemy.root == null) continue;
            float d = Vector3.Distance(from, enemy.root.transform.position);
            if (d < bestDistance) { best = enemy; bestDistance = d; }
        }
        return best;
    }

    private void Deploy(string role)
    {
        if (ended) return;
        int summonCount = role == "ranged" ? 2 : 1;
        if (allies.Count + summonCount > 7) return;
        float cost = role == "assault" ? 9f : 5f;
        if (mana < cost) return;
        mana -= cost;
        string name = role == "tank" ? "Graveguard" : role == "ranged" ? "Skeleton Archer" : role == "healer" ? "Troll Healer" : "Gloam Imp";
        Color cloth = role == "tank" ? new Color(0.40f, 0.39f, 0.45f) : role == "ranged" ? new Color(0.26f, 0.48f, 0.40f) : role == "healer" ? new Color(0.35f, 0.50f, 0.30f) : new Color(0.53f, 0.27f, 0.30f);
        for (int i = 0; i < summonCount; i++)
        {
            int slot = allies.Count;
            float side = slot % 2 == 0 ? -1f : 1f;
            float row = slot / 2;
            Vector3 position = hero.root.transform.position + new Vector3(side * (1.5f + row * 0.45f), 0f, -1.4f - row * 1.5f);
            Color skin = role == "ranged" ? new Color(0.76f, 0.72f, 0.63f) : role == "healer" ? new Color(0.44f, 0.63f, 0.39f) : new Color(0.80f, 0.68f, 0.58f);
            Unit ally = CreateUnit(name, false, false, role, cloth, skin, position);
            ally.hp = ally.maxHp = role == "tank" ? 110f : role == "ranged" ? 48f : role == "healer" ? 72f : 62f;
            ally.speed = role == "assault" ? 5.3f : 3.8f;
            ally.damage = role == "tank" ? 11f : role == "ranged" ? 12f : role == "healer" ? 2f : 18f;
            ally.range = role == "ranged" ? 8f : 1.7f;
            ally.attackRate = role == "assault" ? 0.65f : 1f;
            allies.Add(ally);
        }
    }

    private void CastCinderburst()
    {
        Unit focus = selectedEnemy != null ? selectedEnemy : FindClosestEnemy(hero.root.transform.position);
        if (ended || mana < 2f || focus == null) return;
        mana -= 2f;
        Vector3 center = focus.root.transform.position;
        CreateImpactEffect(center + Vector3.up * 0.35f, new Color(1f, 0.28f, 0.06f), new Vector3(4.4f, 1.2f, 4.4f), 0.42f);
        foreach (Unit enemy in enemies)
            if (Vector3.Distance(enemy.root.transform.position, center) <= 3.5f) enemy.hp -= 42f;
    }

    private void CastRimeVeil()
    {
        if (ended || mana < 5f) return;
        mana -= 5f;
        CreateImpactEffect(hero.root.transform.position + Vector3.up * 0.12f, new Color(0.26f, 0.70f, 1f), new Vector3(4.6f, 0.25f, 4.6f), 0.55f);
        hero.hp = Mathf.Min(hero.maxHp, hero.hp + 22f);
        foreach (Unit ally in allies) ally.hp = Mathf.Min(ally.maxHp, ally.hp + 22f);
    }

    private Unit CreateUnit(string name, bool enemy, bool isHero, string role, Color cloth, Color skin, Vector3 position)
    {
        GameObject root = new GameObject(name);
        root.transform.position = position;
        Unit unit = new Unit { root = root, isEnemy = enemy, isHero = isHero, role = role };
        float scale = isHero ? 1f : enemy ? 0.88f : role == "healer" ? 1.12f : role == "assault" ? 0.78f : 0.9f;
        unit.visualScale = scale;
        CapsuleCollider unitCollider = root.AddComponent<CapsuleCollider>();
        unitCollider.center = new Vector3(0f, 1f * scale, 0f);
        unitCollider.height = 2.1f * scale;
        unitCollider.radius = 0.48f * scale;
        Material outfit = MakeMaterial(cloth);
        Material skinMaterial = MakeMaterial(skin);
        Material darkLeather = MakeMaterial(new Color(0.12f, 0.10f, 0.12f));
        Material armor = MakeMetal(new Color(0.31f, 0.34f, 0.39f), 0.72f);
        Material accent = MakeGlow(enemy ? new Color(0.74f, 0.16f, 0.25f) : new Color(0.28f, 0.68f, 0.78f), 1.1f);
        unit.upperBody = CreatePivot("Upper body pivot", position + Vector3.up * 0.72f * scale, root.transform);
        unit.torso = MakePrimitive("Torso", PrimitiveType.Capsule, position + Vector3.up * 1.0f * scale, new Vector3(0.48f, 0.58f, 0.38f) * scale, outfit, unit.upperBody).transform;
        MakePrimitive("Chest armor", PrimitiveType.Cube, position + new Vector3(0f, 1.12f, -0.06f) * scale, new Vector3(0.38f, 0.43f, 0.12f) * scale, armor, unit.upperBody);
        MakePrimitive(enemy || role == "ranged" ? "Skull" : "Head", PrimitiveType.Sphere, position + Vector3.up * 1.85f * scale, new Vector3(0.34f, 0.4f, 0.34f) * scale, skinMaterial, unit.upperBody);
        if (isHero)
        {
            Material hair = MakeMaterial(new Color(0.36f, 0.10f, 0.075f));
            MakePrimitive("Long red hair", PrimitiveType.Sphere, position + new Vector3(0f, 2.03f, 0.12f) * scale, new Vector3(0.42f, 0.40f, 0.39f) * scale, hair, unit.upperBody);
            MakePrimitive("Hair lock left", PrimitiveType.Capsule, position + new Vector3(-0.29f, 1.43f, 0.15f) * scale, new Vector3(0.11f, 0.63f, 0.12f) * scale, hair, unit.upperBody);
            MakePrimitive("Hair lock right", PrimitiveType.Capsule, position + new Vector3(0.29f, 1.43f, 0.15f) * scale, new Vector3(0.11f, 0.63f, 0.12f) * scale, hair, unit.upperBody);
            MakePrimitive("Ranger mantle", PrimitiveType.Capsule, position + new Vector3(0f, 0.93f, 0.27f) * scale, new Vector3(0.37f, 0.58f, 0.13f) * scale, outfit, unit.upperBody);
            MakePrimitive("Ranger mantle clasp", PrimitiveType.Sphere, position + new Vector3(0f, 1.43f, -0.29f) * scale, new Vector3(0.12f, 0.14f, 0.05f) * scale, MakeMetal(new Color(0.78f, 0.54f, 0.24f), 0.65f), unit.upperBody);
            MakePrimitive("Ranger chest strap", PrimitiveType.Cube, position + new Vector3(-0.12f, 1.18f, -0.145f) * scale, new Vector3(0.08f, 0.54f, 0.035f) * scale, darkLeather, unit.upperBody).transform.localRotation = Quaternion.Euler(0f, 0f, -13f);
            MakePrimitive("Ranger bracer", PrimitiveType.Cylinder, position + new Vector3(0.39f, 0.91f, -0.01f) * scale, new Vector3(0.19f, 0.13f, 0.19f) * scale, armor, unit.upperBody);
            MakePrimitive("Arrow quiver", PrimitiveType.Cylinder, position + new Vector3(-0.27f, 1.35f, 0.24f) * scale, new Vector3(0.11f, 0.36f, 0.11f) * scale, darkLeather, unit.upperBody).transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
        }
        else
        {
            MakePrimitive("Hood", PrimitiveType.Sphere, position + new Vector3(0f, 2.01f, 0.08f) * scale, new Vector3(0.39f, 0.4f, 0.38f) * scale, outfit, unit.upperBody);
        }
        unit.leftArm = CreatePivot("Left shoulder pivot", position + new Vector3(-0.39f, 1.42f, 0f) * scale, unit.upperBody);
        unit.rightArm = CreatePivot("Right shoulder pivot", position + new Vector3(0.39f, 1.42f, 0f) * scale, unit.upperBody);
        MakePrimitive("Left arm", PrimitiveType.Capsule, position + new Vector3(-0.39f, 1.12f, 0f) * scale, new Vector3(0.17f, 0.43f, 0.17f) * scale, skinMaterial, unit.leftArm);
        MakePrimitive("Right arm", PrimitiveType.Capsule, position + new Vector3(0.39f, 1.12f, 0f) * scale, new Vector3(0.17f, 0.43f, 0.17f) * scale, skinMaterial, unit.rightArm);
        MakePrimitive("Left shoulder guard", PrimitiveType.Sphere, position + new Vector3(-0.4f, 1.42f, 0f) * scale, new Vector3(0.29f, 0.22f, 0.3f) * scale, armor, unit.upperBody);
        MakePrimitive("Right shoulder guard", PrimitiveType.Sphere, position + new Vector3(0.4f, 1.42f, 0f) * scale, new Vector3(0.29f, 0.22f, 0.3f) * scale, armor, unit.upperBody);
        unit.leftLeg = CreatePivot("Left hip pivot", position + new Vector3(-0.16f, 0.72f, 0f) * scale, unit.upperBody);
        unit.rightLeg = CreatePivot("Right hip pivot", position + new Vector3(0.16f, 0.72f, 0f) * scale, unit.upperBody);
        MakePrimitive("Left leg", PrimitiveType.Capsule, position + new Vector3(-0.16f, 0.38f, 0f) * scale, new Vector3(0.19f, 0.4f, 0.2f) * scale, darkLeather, unit.leftLeg);
        MakePrimitive("Right leg", PrimitiveType.Capsule, position + new Vector3(0.16f, 0.38f, 0f) * scale, new Vector3(0.19f, 0.4f, 0.2f) * scale, darkLeather, unit.rightLeg);
        MakePrimitive("Belt", PrimitiveType.Cylinder, position + new Vector3(0f, 0.74f, 0f) * scale, new Vector3(0.32f, 0.07f, 0.3f) * scale, darkLeather, unit.upperBody);
        MakePrimitive("Belt clasp", PrimitiveType.Sphere, position + new Vector3(0f, 0.75f, -0.29f) * scale, new Vector3(0.10f, 0.11f, 0.05f) * scale, MakeMetal(new Color(0.72f, 0.48f, 0.20f), 0.55f), unit.upperBody);
        MakePrimitive("Eyes", PrimitiveType.Sphere, position + new Vector3(0f, 1.89f, -0.30f) * scale, new Vector3(0.16f, 0.055f, 0.035f) * scale, accent, unit.upperBody);
        if (enemy || role == "ranged")
        {
            Material eyeGlow = MakeGlow(new Color(0.95f, 0.12f, 0.08f), 2f);
            MakePrimitive("Left eye ember", PrimitiveType.Sphere, position + new Vector3(-0.12f, 1.91f, -0.30f) * scale, new Vector3(0.075f, 0.07f, 0.04f) * scale, eyeGlow, unit.upperBody);
            MakePrimitive("Right eye ember", PrimitiveType.Sphere, position + new Vector3(0.12f, 1.91f, -0.30f) * scale, new Vector3(0.075f, 0.07f, 0.04f) * scale, eyeGlow, unit.upperBody);
        }
        if (role == "assault")
        {
            MakePineCrown("Imp horn left", position + new Vector3(-0.19f, 2.10f, 0.02f) * scale, 0.18f * scale, 0.36f * scale, MakeMaterial(new Color(0.18f, 0.10f, 0.12f)), unit.upperBody);
            MakePineCrown("Imp horn right", position + new Vector3(0.19f, 2.10f, 0.02f) * scale, 0.18f * scale, 0.36f * scale, MakeMaterial(new Color(0.18f, 0.10f, 0.12f)), unit.upperBody);
        }
        if (enemy || role == "tank")
        {
            MakePrimitive("Weapon haft", PrimitiveType.Cylinder, position + new Vector3(0.54f, 0.84f, -0.03f) * scale, new Vector3(0.045f, 0.72f, 0.045f) * scale, darkLeather, unit.rightArm).transform.localRotation = Quaternion.Euler(0f, 0f, 24f);
            MakePrimitive("Weapon blade", PrimitiveType.Cube, position + new Vector3(0.83f, 1.42f, -0.03f) * scale, new Vector3(0.12f, 0.62f, 0.055f) * scale, armor, unit.rightArm).transform.localRotation = Quaternion.Euler(0f, 0f, -24f);
        }
        else if (role == "ranger" || role == "ranged")
        {
            MakePrimitive("Bow", PrimitiveType.Cylinder, position + new Vector3(0.56f, 1.13f, 0.03f) * scale, new Vector3(0.055f, 0.64f, 0.055f) * scale, MakeMetal(new Color(0.46f, 0.25f, 0.12f), 0.25f), unit.rightArm).transform.localRotation = Quaternion.Euler(0f, 0f, -12f);
        }
        else if (role == "healer")
        {
            MakePrimitive("Healer staff", PrimitiveType.Cylinder, position + new Vector3(0.62f, 1.16f, 0.02f) * scale, new Vector3(0.06f, 0.92f, 0.06f) * scale, darkLeather, unit.rightArm).transform.localRotation = Quaternion.Euler(0f, 0f, -10f);
            MakePrimitive("Staff crystal", PrimitiveType.Sphere, position + new Vector3(0.73f, 2.02f, 0.02f) * scale, new Vector3(0.20f, 0.23f, 0.20f) * scale, MakeGlow(new Color(0.30f, 0.95f, 0.56f), 2.5f), unit.rightArm);
        }
        MakePrimitive("Ground shadow", PrimitiveType.Cylinder, position + Vector3.up * 0.025f, new Vector3(0.7f, 0.025f, 0.42f) * scale, MakeMaterial(new Color(0.045f, 0.04f, 0.045f)), root.transform);
        if (enemy)
        {
            unit.selectionDisc = MakePrimitive("Target marker", PrimitiveType.Cylinder, position + Vector3.up * 0.055f, new Vector3(1.35f, 0.015f, 1.0f), MakeGlow(new Color(0.95f, 0.22f, 0.16f), 1.2f), root.transform);
            unit.selectionDisc.SetActive(false);
        }
        unit.phase = Random.value * 5f;
        unit.speed = 3.5f;
        unit.range = 1.7f;
        unit.damage = 8f;
        unit.attackRate = 1f;
        unit.hp = unit.maxHp = 50f;
        unitsByCollider[unitCollider] = unit;
        return unit;
    }

    private void Animate(Unit unit, float dt, bool moving)
    {
        unit.phase += dt * (moving ? 11f : 3f);
        unit.attackTime = Mathf.Max(0f, unit.attackTime - dt);
        float stride = moving ? Mathf.Sin(unit.phase) * 34f : 0f;
        float attackProgress = unit.attackTime > 0f ? 1f - unit.attackTime / 0.36f : 0f;
        float swing = attackProgress > 0f ? Mathf.Sin(attackProgress * Mathf.PI) : 0f;
        float idleBreath = Mathf.Sin(unit.phase) * 1.4f;
        unit.leftLeg.localRotation = Quaternion.Euler(stride, 0f, 0f);
        unit.rightLeg.localRotation = Quaternion.Euler(-stride, 0f, 0f);
        unit.leftArm.localRotation = Quaternion.Euler(-stride * 0.55f - idleBreath, 0f, swing * 10f);
        unit.rightArm.localRotation = Quaternion.Euler(stride * 0.55f - swing * 112f - idleBreath, 0f, -swing * 8f);
        unit.upperBody.localRotation = Quaternion.Euler(-swing * 9f + (moving ? Mathf.Sin(unit.phase * 2f) * 2f : idleBreath * 0.25f), 0f, moving ? Mathf.Sin(unit.phase) * 2f : 0f);
        unit.torso.localRotation = Quaternion.identity;
        unit.torso.localPosition = new Vector3(0f, 0.28f * unit.visualScale + (moving ? Mathf.Abs(Mathf.Sin(unit.phase)) * 0.055f : Mathf.Sin(unit.phase) * 0.015f), 0f);
    }

    private Transform CreatePivot(string name, Vector3 position, Transform parent)
    {
        GameObject pivot = new GameObject(name);
        pivot.transform.SetParent(parent, true);
        pivot.transform.position = position;
        return pivot.transform;
    }

    private GameObject MakePrimitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material, Transform parent = null)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.position = position;
        obj.transform.localScale = scale;
        if (parent != null) obj.transform.SetParent(parent, true);
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;
        Collider collider = obj.GetComponent<Collider>();
        if (collider != null && name != "Trail tile") Destroy(collider);
        return obj;
    }

    private Material MakeMaterial(Color color)
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        Material material = new Material(shader) { color = color };
        materials.Add(material);
        return material;
    }

    private Material MakeMetal(Color color, float metallic)
    {
        Material material = MakeMaterial(color);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.62f);
        return material;
    }

    private Material MakeGlow(Color color, float intensity)
    {
        Material material = MakeMaterial(color);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * intensity);
        }
        return material;
    }

    private Texture2D MakeGroundTexture()
    {
        const int size = 256;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGB24, false);
        texture.name = "Ashen trail ground";
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float broad = Mathf.PerlinNoise(x * 0.028f, y * 0.028f);
            float fine = Mathf.PerlinNoise(x * 0.15f + 17f, y * 0.15f + 31f);
            float grain = (Mathf.PerlinNoise(x * 0.37f + 73f, y * 0.37f + 11f) - 0.5f) * 0.09f;
            float value = Mathf.Clamp(0.67f + (broad - 0.5f) * 0.32f + (fine - 0.5f) * 0.18f + grain, 0.35f, 0.9f);
            texture.SetPixel(x, y, new Color(value, value * 0.91f, value * 0.78f));
        }
        texture.Apply(false, true);
        return texture;
    }

    private Mesh CreateConeMesh()
    {
        const int segments = 10;
        Vector3[] vertices = new Vector3[segments + 2];
        int[] triangles = new int[segments * 6];
        vertices[0] = Vector3.zero;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle) * 0.5f, 0f, Mathf.Sin(angle) * 0.5f);
        }
        vertices[segments + 1] = Vector3.up;
        for (int i = 0; i < segments; i++)
        {
            int current = i + 1;
            int next = (i + 1) % segments + 1;
            int offset = i * 6;
            triangles[offset] = current; triangles[offset + 1] = segments + 1; triangles[offset + 2] = next;
            triangles[offset + 3] = 0; triangles[offset + 4] = next; triangles[offset + 5] = current;
        }
        Mesh mesh = new Mesh { name = "Low poly pine crown" };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private void MakePineCrown(string name, Vector3 position, float width, float height, Material material, Transform parent = null)
    {
        GameObject crown = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        crown.transform.position = position;
        crown.transform.localScale = new Vector3(width, height, width);
        if (parent != null) crown.transform.SetParent(parent, true);
        crown.GetComponent<MeshFilter>().sharedMesh = pineConeMesh;
        crown.GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    private void Panel(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 offsetMin, Vector2 offsetMax, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(canvas.transform, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.pivot = pivot; rect.offsetMin = offsetMin; rect.offsetMax = offsetMax;
        obj.GetComponent<Image>().color = color;
    }

    private Text AddText(string name, string value, int size, TextAnchor align, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(canvas.transform, false);
        RectTransform rect = obj.GetComponent<RectTransform>(); rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        Text text = obj.GetComponent<Text>(); text.text = value; text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = size; text.alignment = align; text.color = color; text.raycastTarget = false;
        return text;
    }

    private Image AddBar(string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        GameObject back = new GameObject(name + " background", typeof(RectTransform), typeof(Image));
        back.transform.SetParent(canvas.transform, false);
        RectTransform rect = back.GetComponent<RectTransform>(); rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        back.GetComponent<Image>().color = new Color(0.08f, 0.07f, 0.09f, 0.9f);
        GameObject fill = new GameObject(name, typeof(RectTransform), typeof(Image)); fill.transform.SetParent(back.transform, false);
        RectTransform fillRect = fill.GetComponent<RectTransform>(); fillRect.anchorMin = Vector2.zero; fillRect.anchorMax = Vector2.one; fillRect.offsetMin = new Vector2(2f, 2f); fillRect.offsetMax = new Vector2(-2f, -2f);
        Image image = fill.GetComponent<Image>(); image.color = color; image.type = Image.Type.Filled; image.fillMethod = Image.FillMethod.Horizontal;
        return image;
    }

    private void AddCard(string title, string subtitle, int cost, int index, Color color, UnityEngine.Events.UnityAction action)
    {
        GameObject card = new GameObject("Card " + title, typeof(RectTransform), typeof(Image), typeof(Button));
        card.transform.SetParent(canvas.transform, false);
        RectTransform rect = card.GetComponent<RectTransform>();
        float width = 0.145f;
        float gap = 0.01f;
        float total = width * 6f + gap * 5f;
        float xMin = (1f - total) * 0.5f + index * (width + gap);
        rect.anchorMin = new Vector2(xMin, 0.014f); rect.anchorMax = new Vector2(xMin + width, 0.132f); rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        Image cardImage = card.GetComponent<Image>();
        cardImage.color = new Color(0.72f, 0.55f, 0.29f, 1f);
        Button button = card.GetComponent<Button>(); button.targetGraphic = card.GetComponent<Image>(); button.onClick.AddListener(action);
        ColorBlock states = button.colors;
        states.highlightedColor = new Color(1f, 0.91f, 0.72f, 1f);
        states.pressedColor = new Color(0.68f, 0.54f, 0.40f, 1f);
        button.colors = states;
        cardButtons.Add(button);
        cardCosts.Add(cost);
        GameObject face = new GameObject("Card face", typeof(RectTransform), typeof(Image));
        face.transform.SetParent(card.transform, false);
        RectTransform faceRect = face.GetComponent<RectTransform>(); faceRect.anchorMin = Vector2.zero; faceRect.anchorMax = Vector2.one; faceRect.offsetMin = new Vector2(2f, 2f); faceRect.offsetMax = new Vector2(-2f, -2f);
        face.GetComponent<Image>().color = new Color(color.r * 0.52f, color.g * 0.52f, color.b * 0.52f, 1f);
        face.GetComponent<Image>().raycastTarget = false;
        GameObject crest = new GameObject("Card crest", typeof(RectTransform), typeof(Image));
        crest.transform.SetParent(face.transform, false);
        RectTransform crestRect = crest.GetComponent<RectTransform>(); crestRect.anchorMin = new Vector2(0f, 0.88f); crestRect.anchorMax = new Vector2(1f, 1f); crestRect.offsetMin = Vector2.zero; crestRect.offsetMax = Vector2.zero;
        crest.GetComponent<Image>().color = color;
        crest.GetComponent<Image>().raycastTarget = false;
        string cardTitle = title.Replace(" ", "\n");
        Text label = AddText("Card label " + index, cardTitle + "\n" + cost + " MANA\n" + subtitle, 8, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one, Color.white);
        label.transform.SetParent(card.transform, false);
        RectTransform labelRect = label.GetComponent<RectTransform>(); labelRect.anchorMin = Vector2.zero; labelRect.anchorMax = Vector2.one; labelRect.offsetMin = new Vector2(2f, 2f); labelRect.offsetMax = new Vector2(-2f, -2f);
        label.raycastTarget = false;
    }

    private void UpdateHud()
    {
        if (healthText == null) return;
        healthText.text = "HERO  " + Mathf.CeilToInt(hero.hp) + " / " + Mathf.CeilToInt(hero.maxHp);
        manaText.text = "MANA  " + Mathf.FloorToInt(mana) + " / 26";
        healthFill.fillAmount = Mathf.Clamp01(hero.hp / hero.maxHp);
        manaFill.fillAmount = mana / 26f;
        objectiveText.text = "WRAITHS: " + kills + " / 15   ·   ALLIES: " + allies.Count;
        for (int i = 0; i < cardButtons.Count; i++) cardButtons[i].interactable = mana >= cardCosts[i];
        int seconds = Mathf.CeilToInt(timeLeft);
        timerText.text = (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
    }

    private void EndGame(bool won)
    {
        if (ended) return;
        ended = true;
        Panel("Result shade", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.035f, 0.025f, 0.045f, 0.84f));
        AddText("Result", won ? "THE TRAIL BREAKS\nTHE BELLS FALL SILENT" : "ASHES CLAIM YOU", 22, TextAnchor.MiddleCenter, new Vector2(0.08f, 0.36f), new Vector2(0.92f, 0.62f), new Color(0.90f, 0.76f, 0.53f));
    }

    private void OnDestroy()
    {
        foreach (Material material in materials) if (material != null) Destroy(material);
        if (pineConeMesh != null) Destroy(pineConeMesh);
    }
}
