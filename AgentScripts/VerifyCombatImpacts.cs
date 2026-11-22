// Run in Play Mode with Unity Pipeline: script eval --file AgentScripts/VerifyCombatImpacts.cs --timeout 90
using Hackaton.Combat;
using Hackaton.Enemies;
using Hackaton.Player;
using UnityEngine.AI;

if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
var origin = new Vector3(1000, 50, 1000);
var root = new GameObject("Combat verification");
var observations = new Dictionary<string, object>();
NavMeshData nav = null;
NavMeshDataInstance instance = default;
float previousDelta = Time.captureDeltaTime;
GameObject Spawn(string path, Vector3 offset, float yaw = 0)
{
    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/" + path + ".prefab");
    return Object.Instantiate(prefab, origin + offset, Quaternion.Euler(0, yaw, 0), root.transform);
}
void Check(bool condition, string description)
{
    if (!condition) throw new Exception(description);
}
async Task Seconds(float seconds)
{
    float until = Time.time + seconds;
    await Wait.Until(() => Time.time >= until, timeoutMs: 15000);
}
async Task<(float rise, float travel)> Swing(PlayerAttack attack, EnemyBase enemy, int expectedDamage)
{
    var agent = enemy.GetComponent<NavMeshAgent>();
    Vector3 start = enemy.transform.position;
    int health = enemy.Health;
    Check(attack.TryAttack() && !attack.TryAttack(), "Attack acceptance or cooldown failed.");
    await Wait.Until(() => !agent.updatePosition, timeoutMs: 5000);
    Check(agent.isStopped, "Agent kept steering during impact.");
    float peak = start.y;
    float travel = 0f;
    await Wait.Until(() => {
        var delta = enemy.transform.position - start;
        peak = Mathf.Max(peak, enemy.transform.position.y);
        travel = Mathf.Max(travel, Vector3.ProjectOnPlane(delta, Vector3.up).magnitude);
        return agent.updatePosition;
    }, timeoutMs: 10000);
    Check(enemy.Health == health - expectedDamage, $"Expected one hit for {expectedDamage}, health {health} -> {enemy.Health}");
    Check(agent.isOnNavMesh && Mathf.Abs(enemy.transform.position.y - origin.y) < .25f, "Landing missed the floor or NavMesh.");
    return (peak - start.y, travel);
}

try
{
    Time.captureDeltaTime = 1f / 60f;
    var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
    floor.transform.SetParent(root.transform);
    floor.transform.position = origin + Vector3.down * .5f;
    floor.transform.localScale = new Vector3(30, 1, 30);
    var source = new NavMeshBuildSource { shape = NavMeshBuildSourceShape.Box,
        size = floor.transform.localScale, transform = Matrix4x4.Translate(floor.transform.position) };
    nav = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByID(0), new List<NavMeshBuildSource> {source},
        new Bounds(origin, new Vector3(32, 10, 32)), Vector3.zero, Quaternion.identity);
    instance = NavMesh.AddNavMeshData(nav);
    var hands = Spawn("PlayerTizaManosVacias", Vector3.zero);
    var enemy = Spawn("Enemies/EnemyBase", Vector3.forward * 1.12f, 180).GetComponent<EnemyBase>();
    var agent = enemy.GetComponent<NavMeshAgent>();
    var extra = new GameObject("Second hurt collider");
    extra.transform.SetParent(enemy.transform, false);
    extra.transform.localPosition = Vector3.up * 1.5f;
    extra.AddComponent<SphereCollider>().radius = .3f;
    await Seconds(.15f);
    Vector3 playerStart = hands.transform.position;
    var slap = await Swing(hands.GetComponent<SlapAttack>(), enemy, 0);
    observations["slap"] = new { slap.rise, slap.travel, enemy.Health };
    Check(slap.rise > .85f && slap.travel > 3 && Vector3.Distance(hands.transform.position, playerStart) < .05f,
        $"Slap trajectory or attacker displacement: {slap}, player {hands.transform.position - playerStart}");
    float landedAt = enemy.transform.position.z;
    await Seconds(.35f);
    Check(enemy.State == EnemyBase.EnemyState.Chase && enemy.transform.position.z < landedAt - .15f, "Chase did not resume.");
    observations["chaseAfterLanding"] = true;
    hands.SetActive(false);

    var wielder = Spawn("PlayerTizaBaston", Vector3.zero);
    var staff = wielder.GetComponent<BastonAttack>();
    void FaceTarget()
    {
        agent.Warp(origin + Vector3.forward * 1.12f);
        enemy.transform.rotation = Quaternion.Euler(0, 180, 0);
        Physics.SyncTransforms();
    }
    var combos = new List<object>();
    for (int i = 0; i < 4; i++)
    {
        FaceTarget();
        var strike = await Swing(staff, enemy, 10);
        combos.Add(new { step = i + 1, strike.rise, strike.travel, enemy.Health });
        if (i == 2) Check(strike.rise > 1.1f && strike.travel > 4.5f, $"Finisher trajectory {strike}");
        else Check(strike.rise < .25f && strike.travel > (i == 1 ? .21f : .08f)
            && strike.travel < (i == 1 ? .5f : .25f), $"Ground strike {i + 1}: {strike}");
        await Wait.Until(() => staff.RemainingFraction <= 0f);
    }
    observations["staff"] = combos;
    await Seconds(1.05f);
    FaceTarget();
    var afterTimeout = await Swing(staff, enemy, 10);
    Check(afterTimeout.travel < .25f, "Combo did not expire.");
    await Wait.Until(() => staff.RemainingFraction <= 0f);
    wielder.transform.rotation = Quaternion.Euler(0, 180, 0);
    Check(staff.TryAttack(), "Miss was rejected.");
    await Seconds(.38f);
    wielder.transform.rotation = Quaternion.identity;
    FaceTarget();
    var afterMiss = await Swing(staff, enemy, 10);
    Check(afterMiss.rise > 1.1f, "Miss did not advance the combo.");
    observations["timeoutAndMiss"] = true;

    var playerStats = wielder.GetComponent<PlayerStats>();
    int money = playerStats.Money;
    enemy.ApplyKnockback(new Knockback(12, 7, .65f, .12f), Vector3.forward);
    await Seconds(.12f);
    enemy.ReceiveHit(1000, enemy.transform.position, Vector3.forward, new Knockback(15, 8, .8f, .15f), wielder.transform);
    enemy.TakeDamage(1000);
    Check(enemy.State == EnemyBase.EnemyState.Dead && !agent.enabled && playerStats.Money == money + 10,
        "Death or one-time rewards failed.");
    await Wait.Until(() => !enemy.GetComponent<CharacterController>().enabled, timeoutMs: 5000);
    Check(Mathf.Abs(enemy.transform.position.y - origin.y) < .25f, "Corpse stayed airborne.");
    await Wait.Until(() => enemy == null, timeoutMs: 10000);
    enemy = Spawn("Enemies/EnemyBase", Vector3.forward * 1.12f, 180).GetComponent<EnemyBase>();
    await Seconds(.08f);
    await Swing(staff, enemy, 10);
    observations["deathAndNextTarget"] = true;

    Vector3 before = wielder.transform.position;
    playerStats.ApplyKnockback(new Knockback(4, 7, .5f, .1f), Vector3.right);
    await Seconds(.25f);
    Check(wielder.transform.position.y - before.y > .75f && wielder.transform.position.x > before.x + .4f,
        "Player did not receive the impulse.");
    await Seconds(.8f);
    Check(wielder.GetComponent<CharacterController>().isGrounded && playerStats.Health == PlayerStats.MaxHealth,
        "Player did not land with full health.");
    observations["playerLaunchAndLanding"] = true;

    var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
    wall.transform.SetParent(root.transform);
    wall.transform.position = wielder.transform.position + new Vector3(2, 3, 0);
    wall.transform.localScale = new Vector3(.1f, 6, 5);
    Physics.SyncTransforms();
    playerStats.ApplyKnockback(new Knockback(15, 8, .8f, .15f), Vector3.right);
    await Seconds(1.2f);
    Check(wielder.GetComponent<CharacterController>().isGrounded && wielder.transform.position.x < wall.transform.position.x,
        "Launch passed through the wall or failed to land.");

    // Extend only the physical floor, so the next impact deliberately lands outside the baked mesh.
    floor.transform.localScale = new Vector3(60, 1, 60);
    Physics.SyncTransforms();
    agent = enemy.GetComponent<NavMeshAgent>();
    Check(agent.Warp(origin + new Vector3(13, 0, 4)), "Edge setup failed.");
    enemy.ApplyKnockback(new Knockback(15, 8, .8f, .15f), Vector3.right);
    await Seconds(.6f);
    Check(enemy.transform.position.x > origin.x + 16, "Did not clear the navigation boundary.");
    await Seconds(.7f);
    Check(agent.updatePosition && agent.isOnNavMesh && Mathf.Abs(enemy.transform.position.y - origin.y) < .25f,
        "Failed to recover the NavMesh after landing off its edge.");
    observations["wallAndNavMeshEdge"] = true;
    return observations;
}
finally
{
    Time.captureDeltaTime = previousDelta;
    Object.DestroyImmediate(root);
    if (instance.valid) instance.Remove();
    if (nav != null) Object.DestroyImmediate(nav);
}
