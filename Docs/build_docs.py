"""Generates the SwarmLab review documents (PDF). Run: python build_docs.py"""
import os
import hashlib

_md5 = hashlib.md5  # Python 3.8 + this OpenSSL rejects usedforsecurity=; shim it
hashlib.md5 = lambda *a, **k: _md5(*a)
from reportlab.lib.pagesizes import A4
from reportlab.lib import colors
from reportlab.lib.units import mm
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.enums import TA_LEFT
from reportlab.platypus import (BaseDocTemplate, PageTemplate, Frame, Paragraph, Spacer,
                                Table, TableStyle, KeepTogether)

OUT = os.path.dirname(os.path.abspath(__file__))
NAVY = colors.HexColor("#10223d")
TEAL = colors.HexColor("#0e8f8f")
LIGHT = colors.HexColor("#eef5f6")
GREY = colors.HexColor("#5b6670")
LINE = colors.HexColor("#c9d6d9")
GREEN = colors.HexColor("#1f8a4c")
AMBER = colors.HexColor("#b9770e")

PROJECT = "SwarmLab: VR Autonomous Robot Swarm Laboratory"
COURSE = "VR/AR Lab | Experiential Learning (EL) Project"

body = ParagraphStyle("body", fontName="Helvetica", fontSize=10, leading=14.5, textColor=colors.HexColor("#1d2630"), alignment=TA_LEFT, spaceAfter=5)
h1 = ParagraphStyle("h1", fontName="Helvetica-Bold", fontSize=13, leading=17, textColor=NAVY, spaceBefore=12, spaceAfter=5, keepWithNext=1)
title = ParagraphStyle("title", fontName="Helvetica-Bold", fontSize=22, leading=27, textColor=NAVY, spaceAfter=3)
sub = ParagraphStyle("sub", fontName="Helvetica", fontSize=10.5, leading=15, textColor=GREY, spaceAfter=2)
cell = ParagraphStyle("cell", parent=body, fontSize=9, leading=12.5, spaceAfter=0)
cellb = ParagraphStyle("cellb", parent=cell, fontName="Helvetica-Bold")
cellh = ParagraphStyle("cellh", parent=cell, fontName="Helvetica-Bold", textColor=colors.white)
bullet = ParagraphStyle("bullet", parent=body, leftIndent=14, bulletIndent=3, spaceAfter=2.5)
small = ParagraphStyle("small", parent=body, fontSize=8.5, leading=12, textColor=GREY)


def P(t, s=body):
    return Paragraph(t, s)


def bullets(items):
    return [Paragraph(t, bullet, bulletText="•") for t in items]


def table(rows, widths, header=True, zebra=True):
    data = []
    for i, r in enumerate(rows):
        st = cellh if (header and i == 0) else cell
        data.append([c if not isinstance(c, str) else Paragraph(c, st) for c in r])
    t = Table(data, colWidths=widths, repeatRows=1 if header else 0)
    style = [("VALIGN", (0, 0), (-1, -1), "TOP"),
             ("GRID", (0, 0), (-1, -1), 0.5, LINE),
             ("LEFTPADDING", (0, 0), (-1, -1), 6), ("RIGHTPADDING", (0, 0), (-1, -1), 6),
             ("TOPPADDING", (0, 0), (-1, -1), 4.5), ("BOTTOMPADDING", (0, 0), (-1, -1), 4.5)]
    if header:
        style.append(("BACKGROUND", (0, 0), (-1, 0), NAVY))
    if zebra:
        for i in range(1 if header else 0, len(rows)):
            if i % 2 == 0:
                style.append(("BACKGROUND", (0, i), (-1, i), LIGHT))
    t.setStyle(TableStyle(style))
    return t


def info_block():
    rows = [["Project", PROJECT], ["Course", COURSE], ["Team", "_______________________________________"],
            ["Guide / Faculty", "_______________________________________"], ["Date", "26 November 2026"]]
    t = Table([[Paragraph(a, cellb), Paragraph(b, cell)] for a, b in rows], colWidths=[34 * mm, 128 * mm])
    t.setStyle(TableStyle([("BACKGROUND", (0, 0), (0, -1), LIGHT), ("GRID", (0, 0), (-1, -1), 0.5, LINE),
                           ("TOPPADDING", (0, 0), (-1, -1), 4), ("BOTTOMPADDING", (0, 0), (-1, -1), 4)]))
    return t


def on_page(canvas, doc):
    w, h = A4
    canvas.saveState()
    canvas.setFillColor(TEAL)
    canvas.rect(0, h - 8 * mm, w, 8 * mm, stroke=0, fill=1)
    canvas.setFont("Helvetica", 8)
    canvas.setFillColor(GREY)
    canvas.drawString(20 * mm, 11 * mm, "SwarmLab | VR/AR Lab EL Project")
    canvas.drawRightString(w - 20 * mm, 11 * mm, "Page %d" % doc.page)
    canvas.setStrokeColor(LINE)
    canvas.line(20 * mm, 15 * mm, w - 20 * mm, 15 * mm)
    canvas.restoreState()


def build(filename, doc_title, subtitle, story):
    doc = BaseDocTemplate(os.path.join(OUT, filename), pagesize=A4, leftMargin=20 * mm, rightMargin=20 * mm,
                          topMargin=22 * mm, bottomMargin=22 * mm, title=doc_title, author="SwarmLab Team")
    frame = Frame(doc.leftMargin, doc.bottomMargin, doc.width, doc.height, id="f")
    doc.addPageTemplates([PageTemplate(id="p", frames=[frame], onPage=on_page)])
    head = [P(doc_title, title), P(subtitle, sub), Spacer(1, 6), info_block(), Spacer(1, 4)]
    doc.build(head + story)
    print("wrote", filename)


W = 170 * mm

# ---------------------------------------------------------------- 1. OBJECTIVES
obj = [
    P("1. Problem statement", h1),
    P("Swarm robotics, where many simple robots produce intelligent group behaviour through local rules, is hard "
      "to learn and to teach. Real swarms need dozens of robots, which is costly and fragile, and the behaviour "
      "that emerges is difficult to see in static diagrams or 2D plots. Students often study the algorithms without "
      "ever seeing a swarm react to them."),
    P("2. Aim", h1),
    P("To design and develop <b>SwarmLab</b>, an interactive 3D / VR-ready virtual laboratory in Unity where a user "
      "can run, command, disturb and measure swarms of autonomous robots, and so learn swarm behaviour by "
      "experimenting instead of reading."),
    P("3. Objectives", h1),
    table([
        ["ID", "Objective"],
        ["O1", "Build a scalable swarm simulation core able to run 1000+ autonomous agents in real time."],
        ["O2", "Implement five swarm experiments: flocking, rendezvous (consensus), area coverage, "
               "ant-style foraging and formation control."],
        ["O3", "Create an immersive lab environment with first-person interaction, built on the Unity XR "
               "Interaction Toolkit so it is VR-ready."],
        ["O4", "Provide interaction tools that let the user place obstacles and targets, grab robots and inject "
               "faults (robot failure, communication jamming)."],
        ["O5", "Add a live metrics dashboard (coverage, time-to-goal, collisions, connectivity) and a side-by-side "
               "comparison of two algorithms."],
        ["O6", "Add a guided challenge mode and lab-manual style instructions so the project works as a learning tool."],
        ["O7", "Evaluate performance (frame rate) and usability (a short user survey)."],
    ], [14 * mm, 156 * mm]),
    P("4. Scope", h1),
    table([
        ["In scope", "Out of scope"],
        ["Simulated robots in a 3D lab environment<br/>Five swarm experiments with live metrics<br/>"
         "Desktop build with an XR Device Simulator (no headset needed)<br/>Fault injection and challenge mode<br/>"
         "Windows executable build",
         "Physical robot hardware<br/>Headset-specific optimisation (the design stays VR-ready)<br/>"
         "Multiplayer or networked play<br/>Reinforcement-learning training (possible future work)"],
    ], [85 * mm, 85 * mm]),
    P("5. Expected learning outcomes", h1),
] + bullets([
    "Explain how simple local rules produce emergent group behaviour.",
    "Compare swarm algorithms using quantitative metrics instead of intuition.",
    "Describe how a swarm tolerates robot failure and communication loss (robustness and scalability).",
    "Relate the experiments to real applications such as search and rescue, warehouse logistics and environmental monitoring.",
]) + [
    P("6. Why this suits a VR/AR laboratory project", h1),
] + bullets([
    "<b>Spatial understanding:</b> 3D swarm behaviour is understood better when viewed and manipulated in 3D space.",
    "<b>Safe and scalable:</b> hundreds of robots, destructive experiments and repeated runs cost nothing.",
    "<b>Natural interaction:</b> grabbing, placing and pointing replace sliders and code edits.",
    "<b>Engagement:</b> challenges and visual feedback make a technical topic fun to explore.",
]) + [
    P("7. Success criteria", h1),
    table([
        ["Criterion", "Target", "How it is measured"],
        ["Swarm size", "1000+ agents", "Agent counter in the app"],
        ["Performance", "60 FPS on a standard laptop/desktop", "Unity Profiler and in-app FPS counter"],
        ["Experiments", "5 working experiments", "Demonstration of each"],
        ["Metrics", "4+ live metrics, 2-algorithm comparison", "Dashboard output"],
        ["Usability", "SUS score of 70 or above", "Survey of 8-10 classmates"],
    ], [38 * mm, 62 * mm, 70 * mm]),
    P("8. Deliverables", h1),
] + bullets([
    "Unity project with full source code, and a Windows executable build.",
    "Project report and user guide (lab manual).",
    "Demonstration video and final presentation.",
])

# ---------------------------------------------------------------- 2. TOOLS & TECH
tech = [
    P("1. Approach in brief", h1),
    P("SwarmLab is built in Unity (a course requirement) using C#. A simulation core moves the robots, an algorithm "
      "layer decides how each robot behaves, and an interaction layer lets the user play with the swarm. A metrics "
      "layer measures everything. The project targets desktop first and is built on the XR Interaction Toolkit, so it "
      "can run on a headset without redesign."),
    P("2. Technology stack", h1),
    table([
        ["Component", "Tool / technology", "Purpose"],
        ["Game engine", "Unity 6 LTS (Universal Render Pipeline)", "3D rendering, physics, build system"],
        ["Language", "C#", "All simulation, interaction and UI logic"],
        ["VR framework", "XR Interaction Toolkit + XR Device Simulator", "Grab, point and teleport interaction; testing without a headset"],
        ["Input", "Unity Input System", "Keyboard, mouse and controller mapping"],
        ["Performance", "C# Job System + Burst Compiler, GPU instancing", "Run 1000+ agents at real-time speed"],
        ["Visuals", "Shader Graph, VFX Graph, Post-processing", "Neon hologram look, trails, glow"],
        ["User interface", "Unity UI Toolkit / TextMeshPro", "World-space dashboards and wrist menu"],
        ["Code editor", "Visual Studio Community", "Development and debugging"],
        ["Version control", "Git + GitHub", "Source control and backup"],
        ["Analysis", "Unity Profiler, CSV export of metrics", "Performance and experiment results"],
    ], [32 * mm, 64 * mm, 74 * mm]),
    P("3. System architecture", h1),
    table([
        ["Layer", "Responsibility"],
        ["Presentation", "Lab environment, holographic table, dashboards, effects, audio"],
        ["Interaction", "First-person / XR controller, grab tool, obstacle and target placement, wrist menu"],
        ["Experiment manager", "Loads an experiment, sets up scenarios, runs challenge mode and the comparison mode"],
        ["Swarm algorithms", "Flocking, consensus, coverage, foraging and formation behaviours (one module each)"],
        ["Simulation core", "Agent data, neighbour search, movement, collision avoidance, fault injection"],
        ["Metrics", "Collects coverage, time-to-goal, collisions and connectivity, and feeds the dashboard"],
    ], [38 * mm, 132 * mm]),
    P("Each experiment is a separate module behind a common interface, so experiments can be added or removed without "
      "touching the rest of the system.", small),
    P("4. Swarm experiments and algorithms", h1),
    table([
        ["Experiment", "Basis", "What the learner observes"],
        ["Flocking", "Boids model: separation, alignment, cohesion (Reynolds, 1987)", "Birds-like motion emerging from three simple rules"],
        ["Rendezvous", "Distributed consensus (Olfati-Saber et al., 2007)", "Scattered robots converge without a leader"],
        ["Area coverage", "Frontier-based exploration with dispersion", "How fast and how evenly a swarm explores an area"],
        ["Foraging", "Stigmergy and pheromone trails (Dorigo and Stutzle, 2004)", "Shortest paths forming from indirect communication"],
        ["Formation", "Leader-free formation control", "Swarm assembling into shapes and letters"],
    ], [28 * mm, 74 * mm, 68 * mm]),
    P("5. Interaction and controls", h1),
] + bullets([
    "<b>Desktop:</b> WASD and mouse movement, with a grab and place tool; a menu to switch experiments.",
    "<b>Simulated XR:</b> the XR Device Simulator emulates headset and controllers, so the same interaction code is tested without hardware.",
    "<b>Lab tools:</b> place obstacles, targets, attractors and repulsors; kill a percentage of the swarm; jam communication.",
]) + [
    P("6. Performance strategy", h1),
] + bullets([
    "Spatial grid for neighbour lookup, to avoid comparing every robot with every other robot.",
    "Burst-compiled jobs to spread agent updates across CPU cores.",
    "GPU instancing so 1000+ robots render in very few draw calls.",
]) + [
    P("7. System requirements", h1),
    table([
        ["Development machine", "Windows 11 PC/laptop, 16 GB RAM recommended, discrete GPU preferred, about 15 GB free disk"],
        ["Target for demo", "Windows desktop executable (no headset required)"],
    ], [40 * mm, 130 * mm], header=False),
    P("8. Evaluation plan", h1),
] + bullets([
    "<b>Technical:</b> frame rate against agent count, recorded with the Unity Profiler.",
    "<b>Algorithmic:</b> compare experiments on coverage, time-to-goal, collisions and fault recovery, with CSV logs and plots.",
    "<b>Usability:</b> System Usability Scale survey (Brooke, 1996) with 8-10 classmates, plus short feedback comments.",
]) + [
    P("References", h1),
    P("[1] C. Reynolds, \"Flocks, herds and schools: a distributed behavioral model,\" SIGGRAPH, 1987.<br/>"
      "[2] E. Sahin, \"Swarm robotics: from sources of inspiration to domains of application,\" 2005.<br/>"
      "[3] M. Dorigo and T. Stutzle, <i>Ant Colony Optimization</i>, MIT Press, 2004.<br/>"
      "[4] R. Olfati-Saber, J. A. Fax and R. M. Murray, \"Consensus and cooperation in networked multi-agent systems,\" Proc. IEEE, 2007.<br/>"
      "[5] J. Brooke, \"SUS: a quick and dirty usability scale,\" 1996.", small),
]


# ---------------------------------------------------------------- 3. PROGRESS
def status(text, color):
    return Paragraph('<font color="%s"><b>%s</b></font>' % (color.hexval().replace("0x", "#"), text), cell)


prog = [
    P("1. Summary", h1),
    P("The <b>initial stage</b> of the project (problem definition, requirements, design and environment setup) is "
      "complete. The project is now moving into the implementation stage, starting with the core swarm simulation."),
    table([
        ["Current phase", "Phase 1 of 5 completed", "Overall progress", "About 20%"],
    ], [34 * mm, 56 * mm, 36 * mm, 44 * mm], header=False, zebra=False),
    P("2. Phase plan and status", h1),
    table([
        ["Phase", "Description", "Timeline", "Status"],
        ["1", "Initial stage: problem definition, literature survey, requirements, design, environment setup", "Weeks 1-2", status("Completed", GREEN)],
        ["2", "Core swarm simulation and lab environment", "Weeks 3-4", status("Up next", AMBER)],
        ["3", "Experiments and metrics dashboard", "Weeks 5-6", status("Planned", GREY)],
        ["4", "Interaction tools, fault injection and challenge mode", "Weeks 7-8", status("Planned", GREY)],
        ["5", "Testing, user evaluation, polish, documentation and demo", "Weeks 9-10", status("Planned", GREY)],
    ], [14 * mm, 90 * mm, 28 * mm, 38 * mm]),
    P("3. Work completed in the initial stage", h1),
    table([
        ["Task", "Outcome", "Status"],
        ["Topic analysis and problem definition", "Problem statement, aim and objectives documented", status("Done", GREEN)],
        ["Literature survey", "Reviewed swarm robotics fundamentals: flocking, consensus, foraging, formation", status("Done", GREEN)],
        ["Requirements and scope", "In-scope and out-of-scope items finalised; success criteria defined", status("Done", GREEN)],
        ["Experiment selection", "Five experiments chosen with a common module interface", status("Done", GREEN)],
        ["Technology selection", "Unity 6 LTS, C#, XR Interaction Toolkit, Jobs/Burst chosen", status("Done", GREEN)],
        ["System architecture", "Layered architecture designed (simulation, algorithms, interaction, metrics)", status("Done", GREEN)],
        ["Development environment", "Unity Hub and Unity 6 LTS editor with Windows build support set up", status("Done", GREEN)],
        ["Project planning", "Phase plan, milestones, risks and evaluation plan prepared", status("Done", GREEN)],
    ], [48 * mm, 92 * mm, 30 * mm]),
    P("4. Plan for the next stage", h1),
] + bullets([
    "Create the Unity project and repository structure.",
    "Implement the agent simulation core with fast neighbour search.",
    "Build the first two experiments (flocking and rendezvous) on a basic arena.",
    "Build the lab environment and the first-person controller.",
]) + [
    P("5. Risks and mitigation", h1),
    table([
        ["Risk", "Mitigation"],
        ["No VR headset available", "Use the XR Device Simulator; the design stays VR-ready"],
        ["Performance drops with many agents", "Spatial grid, Burst jobs and GPU instancing from the start"],
        ["Scope grows too large", "Modular experiments; build the core first and add extras only if time allows"],
        ["Unity learning curve", "Start with a small prototype; iterate in short, testable steps"],
    ], [60 * mm, 110 * mm]),
]

build("1_Project_Objectives.pdf", "Project Objectives", "Aim, objectives, scope and success criteria", obj)
build("2_Tools_and_Technology.pdf", "Tools and Technology", "Technology stack, architecture and evaluation plan", tech)
build("3_Progress_Report.pdf", "Progress Report", "Status as of 26 November 2026", prog)
