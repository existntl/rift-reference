using System;
using System.Collections.Generic;
using System.Linq;

namespace RiftReference {
public sealed class CoachingCard {
    public string Title, Body;
    public CoachingCard(string title, string body) { Title = title; Body = body; }
}

// Authored educational summaries of Data Dragon 16.17.1 mechanics.
// No state here represents a cast, location, readiness or observed opportunity.
public static class Coaching {
    public static readonly string[] FocusNames = {
        "Support position", "Main threat", "Recall purpose", "Support leaves lane"
    };
    public static string NormalizeFocus(string focus) {
        if (FocusNames.Contains(focus)) return focus;
        if (focus == "Positioning") return "Main threat";
        if (focus == "Wave and macro") return "Recall purpose";
        return "Support position";
    }
    public static string FocusText(string focus) {
        switch (NormalizeFocus(focus)) {
            case "Main threat": return "Before a fight, name the threat you need to answer.";
            case "Recall purpose": return "Before another wave, name why you are staying.";
            case "Support leaves lane": return "When your support leaves, reassess which CS you can approach.";
            default: return "Before trading, check whether your support can contribute.";
        }
    }

    static readonly Dictionary<string,string> Threats = new Dictionary<string,string> {
        {"Nautilus", "Q hook / melee root / R targeted engage"},
        {"Caitlyn", "W trap follow-up / E net and recoil"},
        {"Lulu", "W polymorph / R nearby knock-up"},
        {"Vayne", "E knockback / wall stun; R changes Q"},
        {"LeeSin", "Q gap-close / R kick angle"},
        {"Syndra", "E sphere stun / R burst"},
        {"Darius", "E pull / W slow; extended melee threat"},
        {"Ahri", "E charm / R changes approach angle"},
        {"Ornn", "E terrain knock-up / R redirected engage"},
        {"Vi", "Q dash / R targeted engage"},
        {"Nami", "Q bubble / R wave; E attack slow"},
        {"Braum", "Passive stun stacks / R knock-up"},
        {"Janna", "Q tornado / R knockback"},
        {"Thresh", "Q hook / E displacement"},
        {"Leona", "E engage / Q stun / R area control"},
        {"Blitzcrank", "Q grab / E knock-up / R silence"},
        {"Rakan", "W knock-up / R contact charm"},
        {"Morgana", "Q root / R delayed tether stun"},
        {"Lux", "Q root hits two units / E zone"},
        {"Ezreal", "Q poke / E repositions"},
        {"Lucian", "E dash into follow-up attacks"},
        {"Jhin", "W conditional root / fourth-shot threat"},
        {"Jinx", "E trap line / W slow"},
        {"Ashe", "Attack slows / R arrow engage"},
        {"Sivir", "E spell block / R team movement"},
        {"Samira", "W missile destruction / close-range R"},
        {"Draven", "Q axe attacks / E displacement"},
        {"Tristana", "W jump / E bomb / R knockback"}
    };
    static readonly Dictionary<string,string> Protection = new Dictionary<string,string> {
        {"Lulu", "E shield, W polymorph or ally buff, R health and knock-up"},
        {"Nami", "W heal, Q control and R disengage"},
        {"Braum", "E directional interception, passive stun and R control"},
        {"Janna", "E shield, Q interruption and R knockback"},
        {"Thresh", "W lantern extraction and E displacement"},
        {"Morgana", "E magic shield and control protection while it holds"},
        {"Lux", "W shield and Q root"},
        {"Rakan", "E ally shield and W/R control"},
        {"Nautilus", "Q, passive root and R can help control an attacker"},
        {"Leona", "Q stun and R control can help protect a carry"},
        {"Blitzcrank", "E knock-up and Q displacement"},
        {"Ornn", "E terrain knock-up and R control"},
        {"Ahri", "E charm can interrupt an approach"},
        {"Vi", "Q and R can help lock down an attacker"}
    };
    static readonly Dictionary<string,string> Patterns = new Dictionary<string,string> {
        {"Vayne", "Extend on one target only with a safe exit; preserve Q for the threat."},
        {"Caitlyn", "Compare ranged poke with trap follow-up; avoid chasing past protection."},
        {"Ezreal", "Compare Q poke with a return trade; preserve E when access is unsafe."},
        {"Lucian", "Compare a brief ability-and-attack trade with the cost of dashing in."},
        {"Jhin", "Plan the exit around ammunition; compare a short trade with follow-up."},
        {"Jinx", "Compare rocket poke with sustained attacks when a teammate can protect you."},
        {"Ashe", "Compare short poke with an extended slow chase; check the retreat route."},
        {"Sivir", "Compare wave pressure with a trade; consider which spell E must answer."},
        {"Samira", "Compare allied control follow-up with holding back until access is safer."},
        {"Draven", "Compare axe pressure with the risk of walking into control to catch one."},
        {"Tristana", "Compare bomb commitment with holding the jump for escape."}
    };

    static Player Unique(IEnumerable<Player> players, string role) {
        var matches = players.Where(p => LaneView.Role(p.Role) == role).ToArray();
        return matches.Length == 1 && !String.IsNullOrEmpty(matches[0].Champion) ? matches[0] : null;
    }
    public static bool Supported(Snapshot s) {
        return s.Phase == "ChampSelect" || s.Mode == "CLASSIC";
    }
    public static string Threat(DataStore d, Player player) {
        string text;
        return Threats.TryGetValue(d.Resolve(player.Champion), out text)
            ? text : "Kit not reviewed; consult champion reference";
    }
    public static CoachingCard LanePlan(DataStore d, Snapshot s) {
        var self = s.Players.FirstOrDefault(p => p.Self);
        if (!Supported(s)) return new CoachingCard("LANE PLAN · MODE UNSUPPORTED", "These lane lessons assume Summoner's Rift. General lessons remain in the Playbook.");
        if (self == null || String.IsNullOrEmpty(self.Team)) return new CoachingCard("LANE PLAN · WAITING", "Your champion and team are not identified. No lane pairing has been guessed.");
        string role = LaneView.Role(self.Role);
        if (role != "BOTTOM" && role != "UTILITY") return new CoachingCard("LANE PLAN · GENERAL REFERENCE", "A four-champion plan needs bot-lane roles.\n\nCompare your trade pattern, the wave and who can join. Consult the Playbook for your existing matchup reference.");
        var allies = s.Players.Where(p => p.Team == self.Team);
        var enemies = s.Players.Where(p => p.Team != self.Team && p.Team != "");
        var adc = Unique(allies, "BOTTOM"); var support = Unique(allies, "UTILITY");
        var enemyAdc = Unique(enemies, "BOTTOM"); var enemySupport = Unique(enemies, "UTILITY");
        if (adc == null || support == null || enemyAdc == null || enemySupport == null)
            return new CoachingCard("LANE PLAN · INCOMPLETE", "Waiting for one ADC and one support on each team. Missing or ambiguous roles are not inferred from champion names.\n\nTraining focus and known champion references remain available.");

        string pattern, partner;
        if (!Patterns.TryGetValue(d.Resolve(adc.Champion), out pattern)) pattern = "ADC pattern not authored; compare short trades with the cost of staying in range.";
        if (!Protection.TryGetValue(d.Resolve(support.Champion), out partner)) partner = "Partner tools not reviewed; agree on follow-up and an exit.";
        else partner = d.Name(support.Champion) + ": " + partner + ".";
        string lineup = d.Name(adc.Champion) + " + " + d.Name(support.Champion) + " vs " + d.Name(enemyAdc.Champion) + " + " + d.Name(enemySupport.Champion);
        bool authored = d.Resolve(adc.Champion) == "Vayne" && d.Resolve(support.Champion) == "Lulu" && d.Resolve(enemyAdc.Champion) == "Caitlyn" && d.Resolve(enemySupport.Champion) == "Nautilus";
        if (authored) return new CoachingCard("LANE PLAN · " + lineup,
            "TRADE: Short exchanges first; extend only with Lulu in reach.\n" +
            "PARTNER: Lulu E can shield; W has a buff-versus-peel choice.\n" +
            "DANGER: Nautilus control can set up Caitlyn's traps.\n" +
            "OPENING: A missed hook may allow space; check net and traps.\n" +
            "ABORT: Keep an exit; melee root and targeted R remain threats.");
        return new CoachingCard("LANE PLAN · PATTERN GUIDE",
            lineup + "\n" + pattern + "\n" + partner + "\n" +
            "CHECK: " + d.Name(enemySupport.Champion) + " " + Threat(d, enemySupport) + "; " + d.Name(enemyAdc.Champion) + " " + Threat(d, enemyAdc) + ".\n" +
            "Opening to evaluate: after their control misses, compare follow-up with wave and exit costs.");
    }
    public static CoachingCard ThreatCard(DataStore d, Snapshot s) {
        var self = s.Players.FirstOrDefault(p => p.Self);
        if (self == null || String.IsNullOrEmpty(self.Team)) return new CoachingCard("ENEMY TOOLS · WAITING", "Your team is not identified. No threat roster has been guessed.");
        var enemies = s.Players.Where(p => p.Team != self.Team && p.Team != "").Take(5).ToArray();
        var lines = enemies.Select(p => d.Name(p.Champion) + " · " + Threat(d, p)).ToList();
        if (enemies.Length < 5) lines.Add("Incomplete roster · " + (5 - enemies.Length) + " enemy slot(s) not exposed.");
        return new CoachingCard("ENEMY TOOLS · KIT REFERENCE", String.Join("\n", lines));
    }
    public static string Teamfight(DataStore d, Snapshot s) {
        var self = s.Players.FirstOrDefault(p => p.Self);
        if (self == null || self.Team == "") return "Your team is not identified. Compare threat access, ally protection and your exit before a fight.";
        var protection = new List<string>();
        foreach (var p in s.Players.Where(p => p.Team == self.Team && !p.Self)) {
            string tools;
            protection.Add(d.Name(p.Champion) + ": " + (Protection.TryGetValue(d.Resolve(p.Champion), out tools) ? tools : "Protection tools not reviewed; check the champion kit."));
        }
        string own = d.Resolve(self.Champion) == "Vayne"
            ? "Vayne: compare preserving Q for a new angle with using it for damage. E can create distance; chasing a wall stun can cost your exit."
            : "Your kit: " + d.Tip(self.Champion, false);
        return "BEFORE ENTERING\nWhich engage and follow-up tools could stop you? Compare joining now with waiting for a clearer approach.\n\n" +
            ThreatCard(d, s).Body + "\n\nALLY PROTECTION OPTIONS\n" + String.Join("\n", protection) +
            "\n\nYOUR POSITION\n" + own + "\n\nMOVING FORWARD\nA tool visibly committed elsewhere may change access. Compare a reachable target with the risks of advancing into the next threat. Positions, casts and readiness are not observed by this app.";
    }
    public static CoachingCard[] Lessons(DataStore d, Snapshot s) {
        var matchup = Matchups.Cards(d, LaneView.Select(s), false);
        var reference = new List<string>();
        for (int i = 0; i + 1 < matchup.Length; i += 2) reference.Add(matchup[i] + "\n" + matchup[i+1]);
        return new[] {
            new CoachingCard("Lane plan", LanePlan(d,s).Title + "\n\n" + LanePlan(d,s).Body + "\n\nPattern guides combine authored champion summaries. They do not establish a winning matchup, or observe wave, health, support position or spell use."),
            new CoachingCard("Matchup references", String.Join("\n\n", reference)),
            new CoachingCard("Teamfight plan", Teamfight(d,s)),
            new CoachingCard("Wave and recall", "HOLDING NEAR YOUR TOWER\nCompare easier access to safety with allowing the opponent time to move. Check whether you can actually hold the wave without losing too much health.\n\nBUILDING A PUSH\nA larger wave may support a crash, but staying forward increases exposure. Consider enemy access and your partner's position.\n\nCRASH AND RECALL\nCompare spending now with taking more tower damage or another wave. Verify the crash yourself; an unfinished push may let the opponent hold it.\n\nONE MORE WAVE\nName what it buys. Compare the income with delaying your purchase, return to lane or objective preparation.\n\nSUPPORT LEAVES\nCompare giving up dangerous CS with approaching alone. Experience access can matter even when a last hit is unsafe.\n\nThese are study scenarios. The app does not see the wave or tell you which one applies."),
            new CoachingCard("Convert an advantage", "OPPONENTS RETREAT\nCompare a crash and reset with tower damage. Check health, wave and enemy arrivals before extending.\n\nWIN A FIGHT AT LOW HEALTH\nCompare spending your advantage with staying for a reward that might cost a delayed death.\n\nBOT TOWER FALLS\nCompare available lanes, who can collect them and where you can farm with protection. A tower falling does not automatically require a particular rotation.\n\nOBJECTIVE ACCESS IS LOST\nCompare a delayed contest with available farm, structures or a reset. Consider arrival time and the next objective rather than assuming every contest is mandatory.\n\nThese are alternatives to evaluate, not observed recommendations."),
            new CoachingCard("Training and review", "ONE FOCUS\nChoose one sentence in Preferences before queueing. Keep it for several games if it helps. There is no extra coaching audio.\n\nAFTER THE GAME\nUse Review to save your own answers:\n1. What decision created your first major disadvantage?\n2. What advantage did you fail to convert?\n3. What will you do differently next game?\n\nRecord whether the focus influenced a decision. Notes stay on this PC and can be revisited in Review. No performance score or causal diagnosis is inferred."),
            new CoachingCard("Sources and coverage", "Mechanic summaries: Riot Data Dragon 16.17.1 (bundled). Champion pages checked 2026-09-06 for Vayne, Lulu, Nautilus and Caitlyn. Tactical choices are authored educational interpretations, not Challenger-validated matchup results.\n\nOne authored quartet: Vayne + Lulu versus Caitlyn + Nautilus. Other quartets combine available pattern and tool summaries; unknown champions remain explicitly unreviewed. Threat summaries cover " + Threats.Count + " champions and are not an exhaustive list of their abilities. R references describe kit capabilities, not proof that R is learned or ready.\n\nPatch match remains unverified. Enemy tools are shown in roster order, not ranked by predicted danger. No cast, location, wave, vision or cooldown tracking is added.\n\nRift Ready isn't endorsed by Riot Games and doesn't reflect the views or opinions of Riot Games or anyone officially involved in producing or managing Riot Games properties. Riot Games and all associated properties are trademarks or registered trademarks of Riot Games, Inc.\n\nRegistration/audit is not verified. Policy: developer.riotgames.com/docs/lol and developer.riotgames.com/policies/general.")
        };
    }
}
}
