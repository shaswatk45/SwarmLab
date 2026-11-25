"""SwarmLab controls cheat-sheet (one-page PDF). Run: python controls_sheet.py"""
import os, hashlib
_md5 = hashlib.md5
hashlib.md5 = lambda *a, **k: _md5(*a)
from reportlab.lib.pagesizes import A4
from reportlab.lib import colors
from reportlab.lib.units import mm
from reportlab.lib.styles import ParagraphStyle
from reportlab.platypus import (BaseDocTemplate, PageTemplate, Frame, Paragraph, Spacer,
                                Table, TableStyle)

OUT = os.path.dirname(os.path.abspath(__file__))
NAVY = colors.HexColor("#10223d")
TEAL = colors.HexColor("#0e8f8f")
LIGHT = colors.HexColor("#eef5f6")
GREY = colors.HexColor("#5b6670")
LINE = colors.HexColor("#c9d6d9")
KEYBG = colors.HexColor("#10223d")

title = ParagraphStyle("title", fontName="Helvetica-Bold", fontSize=22, leading=26, textColor=NAVY, spaceAfter=2)
sub = ParagraphStyle("sub", fontName="Helvetica", fontSize=10.5, leading=14, textColor=GREY, spaceAfter=2)
h1 = ParagraphStyle("h1", fontName="Helvetica-Bold", fontSize=12.5, leading=15, textColor=TEAL, spaceBefore=10, spaceAfter=4)
key = ParagraphStyle("key", fontName="Helvetica-Bold", fontSize=10.5, leading=13, textColor=colors.white, alignment=1)
desc = ParagraphStyle("desc", fontName="Helvetica", fontSize=10, leading=13, textColor=colors.HexColor("#1d2630"))
descb = ParagraphStyle("descb", parent=desc, fontName="Helvetica-Bold")
note = ParagraphStyle("note", fontName="Helvetica", fontSize=9, leading=12.5, textColor=GREY)


def on_page(canvas, doc):
    w, h = A4
    canvas.saveState()
    canvas.setFillColor(TEAL)
    canvas.rect(0, h - 8 * mm, w, 8 * mm, stroke=0, fill=1)
    canvas.setFont("Helvetica", 8)
    canvas.setFillColor(GREY)
    canvas.drawString(18 * mm, 10 * mm, "SwarmLab - Controls Reference")
    canvas.restoreState()


def rows(data):
    """data: list of (key, description). Returns a styled 2-col table."""
    body = []
    for k, d in data:
        body.append([Paragraph(k, key), Paragraph(d, desc)])
    t = Table(body, colWidths=[34 * mm, 136 * mm])
    style = [("VALIGN", (0, 0), (-1, -1), "MIDDLE"),
             ("BACKGROUND", (0, 0), (0, -1), KEYBG),
             ("GRID", (0, 0), (-1, -1), 0.5, LINE),
             ("LEFTPADDING", (0, 0), (-1, -1), 7), ("RIGHTPADDING", (0, 0), (-1, -1), 7),
             ("TOPPADDING", (0, 0), (-1, -1), 5), ("BOTTOMPADDING", (0, 0), (-1, -1), 5)]
    for i in range(len(data)):
        if i % 2 == 1:
            style.append(("BACKGROUND", (1, i), (1, i), LIGHT))
    t.setStyle(TableStyle(style))
    return t


doc = BaseDocTemplate(os.path.join(OUT, "4_Controls_CheatSheet.pdf"), pagesize=A4,
                      leftMargin=18 * mm, rightMargin=18 * mm, topMargin=18 * mm, bottomMargin=16 * mm,
                      title="SwarmLab Controls", author="SwarmLab")
frame = Frame(doc.leftMargin, doc.bottomMargin, doc.width, doc.height, id="f")
doc.addPageTemplates([PageTemplate(id="p", frames=[frame], onPage=on_page)])

story = [
    Paragraph("SwarmLab - Controls", title),
    Paragraph("Press Play in Unity, then use the keys below or click the buttons on the dashboard panel.", sub),
    Spacer(1, 4),

    Paragraph("Experiments  (number keys, or the dashboard buttons)", h1),
    rows([
        ("1", "<b>Flocking</b> - boids: separation, alignment, cohesion"),
        ("2", "<b>Rendezvous</b> - leaderless agents converge to one point"),
        ("3", "<b>Area Coverage</b> - swarm explores; floor lights up, coverage % climbs"),
        ("4", "<b>Foraging</b> - ants leave pheromone trails between nest and food"),
        ("5", "<b>Formation</b> - swarm assembles into a shape"),
        ("6", "<b>Search &amp; Rescue</b> - find scattered survivors; mission timer runs"),
        ("F", "Morph to the next shape (Formation only): sphere, ring, grid, heart"),
    ]),

    Paragraph("Fault injection  (show the swarm is resilient)", h1),
    rows([
        ("K", "<b>Kill 30%</b> of the swarm instantly - it keeps working"),
        ("J", "<b>Jam comms</b> (toggle) - agents lose their neighbours; connectivity drops to 0"),
        ("R", "<b>Reset</b> - re-scatter the swarm and clear all faults"),
    ]),

    Paragraph("Environment &amp; data", h1),
    rows([
        ("E", "<b>Export CSV</b> - save the recorded metrics to a file"),
        ("B", "<b>Drop obstacle</b> at the mouse position - the swarm flows around it"),
        ("N", "<b>Clear obstacles</b>"),
    ]),

    Paragraph("Camera &amp; mouse", h1),
    rows([
        ("Left-drag", "<b>Attract</b> the swarm toward the cursor"),
        ("Right-drag", "<b>Repel</b> the swarm away from the cursor"),
        ("Middle-drag", "Rotate the camera around the arena"),
        ("Scroll", "Zoom in / out"),
        ("O", "Toggle the slow auto-orbit"),
    ]),

    Spacer(1, 8),
    Paragraph("CSV files are saved to:  C:\\Users\\91953\\SwarmLab\\SwarmLab_Reports\\   "
              "(open in Excel to plot coverage over time, rescue time, or connectivity vs comms jamming).", note),
    Paragraph("Tip for the demo: Search &amp; Rescue -> drop obstacles -> Kill 30% -> Jam comms -> Export CSV. "
              "That tells a full 'real robots, real problem, real data' story.", note),
]

doc.build(story)
print("wrote 4_Controls_CheatSheet.pdf")
