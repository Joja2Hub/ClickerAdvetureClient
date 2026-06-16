from __future__ import annotations

from pathlib import Path
from typing import Iterable

from PIL import Image, ImageEnhance, ImageFilter
from docx import Document
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.shared import Cm, Pt
from pptx import Presentation
from pptx.dml.color import RGBColor
from pptx.enum.shapes import MSO_CONNECTOR, MSO_SHAPE
from pptx.enum.text import MSO_ANCHOR, PP_ALIGN
from pptx.oxml.xmlchemy import OxmlElement
from pptx.util import Inches, Pt as PptPt


ROOT = Path(__file__).resolve().parents[1]
DOCS = ROOT / "Docs"
TEMP = ROOT / "Temp" / "presentation"
MEDIA = ROOT / "Temp" / "VKR_media"
ART = ROOT / "Assets" / "Resources" / "Art" / "Backgrounds"

PPTX_PATH = DOCS / "Ерюшев_Презентация_ВКР.pptx"
SPEECH_PATH = DOCS / "Ерюшев_Текст_защиты.docx"
SPEECH_MD_PATH = DOCS / "Ерюшев_Текст_защиты.md"

SLIDE_W = Inches(13.333333)
SLIDE_H = Inches(7.5)

INK = RGBColor(25, 39, 52)
MUTED = RGBColor(83, 99, 108)
CREAM = RGBColor(247, 244, 235)
PAPER = RGBColor(255, 253, 248)
GREEN = RGBColor(29, 91, 69)
GREEN_LIGHT = RGBColor(222, 237, 229)
GOLD = RGBColor(204, 143, 37)
GOLD_LIGHT = RGBColor(249, 236, 205)
BLUE = RGBColor(48, 105, 151)
BLUE_LIGHT = RGBColor(224, 237, 247)
PURPLE = RGBColor(101, 73, 151)
PURPLE_LIGHT = RGBColor(235, 229, 247)
RED = RGBColor(174, 74, 55)
RED_LIGHT = RGBColor(247, 226, 221)
WHITE = RGBColor(255, 255, 255)
BLACK = RGBColor(0, 0, 0)

FONT = "Aptos"
FONT_DISPLAY = "Aptos Display"


def rgb_hex(color: RGBColor) -> str:
    return str(color)


def set_shape_alpha(shape, alpha_percent: int) -> None:
    solid_fill = shape.fill._xPr.solidFill
    if solid_fill is None:
        return
    srgb = solid_fill.find("{http://schemas.openxmlformats.org/drawingml/2006/main}srgbClr")
    if srgb is None:
        return
    alpha = OxmlElement("a:alpha")
    alpha.set("val", str(max(0, min(100, alpha_percent)) * 1000))
    srgb.append(alpha)


def add_text(
    slide,
    text: str,
    x: float,
    y: float,
    w: float,
    h: float,
    size: float = 20,
    color: RGBColor = INK,
    bold: bool = False,
    align: PP_ALIGN = PP_ALIGN.LEFT,
    valign: MSO_ANCHOR = MSO_ANCHOR.TOP,
    font: str = FONT,
    margin: float = 0.03,
):
    box = slide.shapes.add_textbox(Inches(x), Inches(y), Inches(w), Inches(h))
    box.text_frame.clear()
    box.text_frame.word_wrap = True
    box.text_frame.margin_left = Inches(margin)
    box.text_frame.margin_right = Inches(margin)
    box.text_frame.margin_top = Inches(margin)
    box.text_frame.margin_bottom = Inches(margin)
    box.text_frame.vertical_anchor = valign
    p = box.text_frame.paragraphs[0]
    p.alignment = align
    p.space_after = PptPt(0)
    run = p.add_run()
    run.text = text
    run.font.name = font
    run.font.size = PptPt(size)
    run.font.bold = bold
    run.font.color.rgb = color
    return box


def add_rich_text(
    slide,
    runs: Iterable[tuple[str, bool, RGBColor]],
    x: float,
    y: float,
    w: float,
    h: float,
    size: float = 20,
    align: PP_ALIGN = PP_ALIGN.LEFT,
):
    box = slide.shapes.add_textbox(Inches(x), Inches(y), Inches(w), Inches(h))
    box.text_frame.clear()
    box.text_frame.word_wrap = True
    p = box.text_frame.paragraphs[0]
    p.alignment = align
    for text, bold, color in runs:
        run = p.add_run()
        run.text = text
        run.font.name = FONT
        run.font.size = PptPt(size)
        run.font.bold = bold
        run.font.color.rgb = color
    return box


def add_rect(
    slide,
    x: float,
    y: float,
    w: float,
    h: float,
    fill: RGBColor,
    line: RGBColor | None = None,
    radius: bool = True,
    transparency: int | None = None,
):
    shape_type = MSO_SHAPE.ROUNDED_RECTANGLE if radius else MSO_SHAPE.RECTANGLE
    shape = slide.shapes.add_shape(shape_type, Inches(x), Inches(y), Inches(w), Inches(h))
    shape.fill.solid()
    shape.fill.fore_color.rgb = fill
    if transparency is not None:
        set_shape_alpha(shape, 100 - transparency)
    shape.line.color.rgb = line or fill
    shape.line.width = PptPt(1)
    return shape


def add_line(slide, x1, y1, x2, y2, color=INK, width=2.0, arrow=False, dash=False):
    line = slide.shapes.add_connector(
        MSO_CONNECTOR.STRAIGHT,
        Inches(x1),
        Inches(y1),
        Inches(x2),
        Inches(y2),
    )
    line.line.color.rgb = color
    line.line.width = PptPt(width)
    if arrow:
        line.line.end_arrowhead = True
    if dash:
        line.line.dash_style = "dash"
    return line


def add_circle(slide, x, y, d, fill, line=None):
    shape = slide.shapes.add_shape(MSO_SHAPE.OVAL, Inches(x), Inches(y), Inches(d), Inches(d))
    shape.fill.solid()
    shape.fill.fore_color.rgb = fill
    shape.line.color.rgb = line or fill
    return shape


def add_picture_crop(slide, path: Path, x: float, y: float, w: float, h: float):
    with Image.open(path) as im:
        iw, ih = im.size
    image_ratio = iw / ih
    box_ratio = w / h
    pic = slide.shapes.add_picture(str(path), Inches(x), Inches(y), width=Inches(w), height=Inches(h))
    if image_ratio > box_ratio:
        visible = box_ratio / image_ratio
        pic.crop_left = (1 - visible) / 2
        pic.crop_right = (1 - visible) / 2
    else:
        visible = image_ratio / box_ratio
        pic.crop_top = (1 - visible) / 2
        pic.crop_bottom = (1 - visible) / 2
    return pic


def add_slide_number(slide, number: int) -> None:
    add_text(slide, f"{number:02d}", 12.62, 7.08, 0.4, 0.2, 9, MUTED, bold=True, align=PP_ALIGN.RIGHT)


def add_header(slide, title: str, subtitle: str | None, number: int) -> None:
    add_rect(slide, 0, 0, 13.333, 7.5, CREAM, radius=False)
    add_rect(slide, 0, 0, 0.16, 7.5, GREEN, radius=False)
    add_text(slide, title, 0.55, 0.35, 11.9, 0.52, 29, INK, bold=True, font=FONT_DISPLAY)
    if subtitle:
        add_text(slide, subtitle, 0.58, 0.94, 11.8, 0.35, 12.5, MUTED)
    add_rect(slide, 0.58, 1.34, 1.05, 0.06, GOLD, radius=False)
    add_slide_number(slide, number)


def add_bullet_list(
    slide,
    items: list[str],
    x: float,
    y: float,
    w: float,
    h: float,
    size: float = 18,
    color: RGBColor = INK,
    bullet_color: RGBColor = GOLD,
    gap: float = 9,
):
    box = slide.shapes.add_textbox(Inches(x), Inches(y), Inches(w), Inches(h))
    tf = box.text_frame
    tf.clear()
    tf.word_wrap = True
    tf.margin_left = 0
    tf.margin_right = 0
    for i, item in enumerate(items):
        p = tf.paragraphs[0] if i == 0 else tf.add_paragraph()
        p.space_after = PptPt(gap)
        p.level = 0
        p.alignment = PP_ALIGN.LEFT
        bullet = p.add_run()
        bullet.text = "●  "
        bullet.font.name = FONT
        bullet.font.size = PptPt(size - 4)
        bullet.font.color.rgb = bullet_color
        run = p.add_run()
        run.text = item
        run.font.name = FONT
        run.font.size = PptPt(size)
        run.font.color.rgb = color
    return box


def add_tag(slide, text: str, x: float, y: float, w: float, fill: RGBColor, color: RGBColor):
    add_rect(slide, x, y, w, 0.38, fill, fill)
    add_text(slide, text, x + 0.06, y + 0.045, w - 0.12, 0.26, 10.5, color, bold=True, align=PP_ALIGN.CENTER)


def prepare_images() -> dict[str, Path]:
    TEMP.mkdir(parents=True, exist_ok=True)
    title_source = ART / "bg_login_adventure_gate.png"
    with Image.open(title_source).convert("RGB") as im:
        canvas = Image.new("RGB", (1920, 1080), (18, 29, 32))
        scale = max(1920 / im.width, 1080 / im.height)
        resized = im.resize((round(im.width * scale), round(im.height * scale)), Image.Resampling.LANCZOS)
        left = (resized.width - 1920) // 2
        top = (resized.height - 1080) // 2
        crop = resized.crop((left, top, left + 1920, top + 1080))
        crop = ImageEnhance.Brightness(crop).enhance(0.46)
        crop = ImageEnhance.Color(crop).enhance(0.72)
        crop.save(TEMP / "title_bg.jpg", quality=92)

    web_login = ROOT / "Temp" / "web_login.png"
    if not web_login.exists():
        web_login = ART / "bg_guild_hall_interior.png"

    unity_strip = Image.new("RGB", (1500, 720), (240, 237, 228))
    sources = [
        ART / "bg_world_map_parchment.png",
        ART / "bg_guild_hall_interior.png",
        ART / "bg_battle_sunny_forest.png",
    ]
    for i, path in enumerate(sources):
        with Image.open(path).convert("RGB") as im:
            im = im.resize((500, 720), Image.Resampling.LANCZOS)
            unity_strip.paste(im, (i * 500, 0))
    unity_strip = unity_strip.filter(ImageFilter.GaussianBlur(0.35))
    unity_strip.save(TEMP / "unity_strip.jpg", quality=90)

    def prepare_diagram(source: Path, target: Path, crop_top: int, ratio: float) -> None:
        with Image.open(source).convert("RGB") as im:
            im = im.crop((0, crop_top, im.width, im.height))
            canvas_w = 1800
            canvas_h = round(canvas_w / ratio)
            canvas = Image.new("RGB", (canvas_w, canvas_h), "white")
            scale = min(canvas_w / im.width, canvas_h / im.height)
            resized = im.resize(
                (round(im.width * scale), round(im.height * scale)),
                Image.Resampling.LANCZOS,
            )
            canvas.paste(
                resized,
                ((canvas_w - resized.width) // 2, (canvas_h - resized.height) // 2),
            )
            canvas.save(target, quality=94)

    prepare_diagram(MEDIA / "image1.png", TEMP / "architecture_clean.jpg", 105, 8.45 / 5.48)
    prepare_diagram(MEDIA / "image3.png", TEMP / "lifecycle_clean.jpg", 175, 8.45 / 5.5)

    return {
        "title": TEMP / "title_bg.jpg",
        "web_login": web_login,
        "unity_strip": TEMP / "unity_strip.jpg",
        "architecture": TEMP / "architecture_clean.jpg",
        "firestore": MEDIA / "image2.png",
        "lifecycle": TEMP / "lifecycle_clean.jpg",
        "analytics": MEDIA / "image4.png",
        "expert": MEDIA / "image5.png",
        "map": ART / "bg_world_map_parchment.png",
        "guild": ART / "bg_guild_hall_interior.png",
        "battle": ART / "bg_battle_sunny_forest.png",
    }


def build_presentation(images: dict[str, Path]) -> None:
    prs = Presentation()
    prs.slide_width = SLIDE_W
    prs.slide_height = SLIDE_H
    blank = prs.slide_layouts[6]

    # 1. Title
    slide = prs.slides.add_slide(blank)
    add_picture_crop(slide, images["title"], 0, 0, 13.333, 7.5)
    overlay = add_rect(slide, 0, 0, 13.333, 7.5, INK, radius=False)
    set_shape_alpha(overlay, 64)
    add_tag(slide, "ВЫПУСКНАЯ КВАЛИФИКАЦИОННАЯ РАБОТА", 0.72, 0.65, 3.7, GOLD_LIGHT, INK)
    add_text(
        slide,
        "Разработка мобильного\nприложения-ежедневника\nс использованием метода\nгеймификации",
        0.72,
        1.42,
        7.9,
        3.1,
        30,
        WHITE,
        bold=True,
        font=FONT_DISPLAY,
    )
    add_text(slide, "Обновленная клиент-серверная система Unity · Web · Firebase", 0.78, 4.72, 7.7, 0.45, 16, GOLD_LIGHT, bold=True)
    add_text(slide, "И. Д. Ерюшев", 9.05, 5.65, 3.3, 0.4, 18, WHITE, bold=True, align=PP_ALIGN.RIGHT)
    add_text(slide, "Направление 02.03.03\nНаучный руководитель: В. А. Акиньшина", 8.45, 6.08, 3.9, 0.72, 12, WHITE, align=PP_ALIGN.RIGHT)
    add_text(slide, "Краснодар · 2026", 0.78, 6.68, 2.4, 0.28, 11, WHITE)
    add_slide_number(slide, 1)

    # 2. Problem and goal
    slide = prs.slides.add_slide(blank)
    add_header(slide, "Проблема, цель и задачи", "От игрового прототипа к семейному цифровому сервису", 2)
    add_rect(slide, 0.58, 1.68, 3.9, 4.95, PAPER, GOLD_LIGHT)
    add_text(slide, "Проблема", 0.88, 1.98, 3.2, 0.38, 21, RED, bold=True)
    add_text(
        slide,
        "Обычный список дел слабо мотивирует ребенка, а отдельная игра не связывает цифровой прогресс с полезными действиями.",
        0.88,
        2.52,
        3.25,
        1.55,
        19,
        INK,
    )
    add_rect(slide, 0.88, 4.45, 3.18, 1.58, RED_LIGHT, RED_LIGHT)
    add_text(slide, "Нужен общий процесс:", 1.08, 4.72, 2.8, 0.32, 15, RED, bold=True)
    add_text(slide, "родитель назначает → ребенок выполняет → система подтверждает и награждает", 1.08, 5.12, 2.72, 0.66, 14, INK)

    add_rect(slide, 4.82, 1.68, 7.93, 2.03, GREEN_LIGHT, GREEN_LIGHT)
    add_text(slide, "Цель", 5.15, 1.96, 1.2, 0.34, 21, GREEN, bold=True)
    add_text(
        slide,
        "Развить Unity-прототип до целостной клиент-серверной системы с web-интерфейсом родителя, синхронизацией заданий, аналитикой и объяснимыми рекомендациями.",
        5.15,
        2.42,
        7.1,
        0.9,
        18,
        INK,
    )
    add_text(slide, "Ключевые задачи", 4.88, 4.06, 3.2, 0.4, 21, INK, bold=True)
    add_bullet_list(
        slide,
        [
            "разделить интерфейсы ребенка и родителя;",
            "реализовать единый жизненный цикл квеста;",
            "расширить облачную модель данных Firestore;",
            "собирать события и рассчитывать аналитику;",
            "формировать рекомендации на базе экспертных правил.",
        ],
        4.95,
        4.58,
        7.25,
        1.75,
        16.5,
        gap=5,
    )

    # 3. Evolution
    slide = prs.slides.add_slide(blank)
    add_header(slide, "Архитектурное развитие проекта", "Главное изменение: система перестала быть изолированным Unity-приложением", 3)
    add_rect(slide, 0.62, 1.76, 4.38, 4.92, BLUE_LIGHT, BLUE)
    add_text(slide, "Исходный MVP", 0.98, 2.06, 3.66, 0.46, 24, BLUE, bold=True, align=PP_ALIGN.CENTER)
    add_picture_crop(slide, images["map"], 1.2, 2.75, 3.2, 1.75)
    add_bullet_list(
        slide,
        ["локальный Unity-клиент", "базовая карта и бой", "локальный список заданий", "минимальная адаптивность"],
        1.02,
        4.86,
        3.65,
        1.4,
        15.5,
        BLUE,
        BLUE,
        gap=5,
    )
    add_line(slide, 5.24, 4.18, 6.48, 4.18, GOLD, 4, arrow=True)
    add_circle(slide, 5.55, 3.65, 0.65, GOLD_LIGHT, GOLD)
    add_text(slide, "+", 5.55, 3.72, 0.65, 0.36, 25, GOLD, bold=True, align=PP_ALIGN.CENTER)
    add_rect(slide, 6.75, 1.76, 5.95, 4.92, GREEN_LIGHT, GREEN)
    add_text(slide, "Итоговая система", 7.15, 2.06, 5.12, 0.46, 24, GREEN, bold=True, align=PP_ALIGN.CENTER)
    for label, x, y, fill, color in [
        ("Unity-клиент\nребенка", 7.12, 2.92, BLUE_LIGHT, BLUE),
        ("Web-панель\nродителя", 9.22, 2.92, GOLD_LIGHT, GOLD),
        ("Firebase\nbackend", 11.02, 2.92, PURPLE_LIGHT, PURPLE),
        ("Аналитика", 8.18, 4.65, PAPER, GREEN),
        ("Экспертные\nрекомендации", 10.28, 4.65, PAPER, GREEN),
    ]:
        add_rect(slide, x, y, 1.55 if x > 10.5 else 1.78, 1.08, fill, color)
        add_text(slide, label, x + 0.08, y + 0.22, (1.39 if x > 10.5 else 1.62), 0.62, 14, color, bold=True, align=PP_ALIGN.CENTER, valign=MSO_ANCHOR.MIDDLE)
    add_text(slide, "Общая модель данных и согласованные статусы", 7.25, 6.02, 4.95, 0.35, 14, GREEN, bold=True, align=PP_ALIGN.CENTER)

    # 4. Architecture
    slide = prs.slides.add_slide(blank)
    add_header(slide, "Общая архитектура системы", "Два пользовательских клиента и единый облачный слой", 4)
    add_picture_crop(slide, images["architecture"], 0.58, 1.58, 8.45, 5.48)
    add_rect(slide, 9.27, 1.58, 3.48, 5.48, PAPER, GREEN_LIGHT)
    add_text(slide, "Распределение ролей", 9.58, 1.93, 2.85, 0.4, 20, GREEN, bold=True)
    add_bullet_list(
        slide,
        [
            "Unity: игровой опыт, выполнение и получение наград.",
            "Web: создание детей, квестов, проверка и аналитика.",
            "Firebase Auth: раздельные роли parent и child.",
            "Firestore: единое состояние семьи и квестов.",
            "Cloud Functions: доверенная бизнес-логика и пересчеты.",
        ],
        9.58,
        2.54,
        2.8,
        3.65,
        15,
        gap=7,
    )
    add_tag(slide, "REST-МОСТ UNITY ↔ FIREBASE", 9.64, 6.33, 2.68, BLUE_LIGHT, BLUE)

    # 5. Unity client
    slide = prs.slides.add_slide(blank)
    add_header(slide, "Обновление Unity-клиента", "Адаптивный интерфейс, сервисная архитектура и общий игровой прогресс", 5)
    add_picture_crop(slide, images["unity_strip"], 0.58, 1.66, 6.55, 2.5)
    for i, title in enumerate(["Карта мира", "Доска квестов", "Боевая сцена"]):
        add_tag(slide, title.upper(), 0.78 + i * 2.18, 3.64, 1.76, PAPER, INK)
    add_rect(slide, 0.58, 4.47, 6.55, 2.15, PAPER, GOLD_LIGHT)
    add_text(slide, "Пользовательские улучшения", 0.88, 4.78, 3.6, 0.38, 19, GOLD, bold=True)
    add_bullet_list(
        slide,
        [
            "Safe Area и масштабирование под мобильные экраны;",
            "интерактивная карта с панорамированием и приближением;",
            "стилизованная доска приключений и карточки квестов;",
            "анимация получения опыта и золота в постоянный HUD.",
        ],
        0.9,
        5.25,
        5.85,
        1.08,
        14.3,
        gap=3,
    )
    add_rect(slide, 7.42, 1.66, 5.32, 4.96, GREEN_LIGHT, GREEN)
    add_text(slide, "Программные слои Unity", 7.78, 1.98, 4.55, 0.38, 20, GREEN, bold=True, align=PP_ALIGN.CENTER)
    layers = [
        ("UI / Scenes", "Login · Map · Battle · Town", BLUE_LIGHT, BLUE),
        ("Services", "Auth · Quest · GameState · Reward · Analytics", PAPER, GREEN),
        ("Domain models", "UserProfile · RealLifeQuest · GameState", GOLD_LIGHT, GOLD),
        ("Data access", "FirebaseRestClient · JSON · Cloud Functions facade", PURPLE_LIGHT, PURPLE),
    ]
    for idx, (title, subtitle, fill, color) in enumerate(layers):
        y = 2.62 + idx * 0.89
        add_rect(slide, 7.92, y, 4.32, 0.7, fill, color)
        add_text(slide, title, 8.12, y + 0.1, 1.35, 0.24, 14.5, color, bold=True)
        add_text(slide, subtitle, 9.47, y + 0.09, 2.55, 0.34, 11.7, INK, valign=MSO_ANCHOR.MIDDLE)
    add_text(slide, "ServiceRegistry связывает подсистемы и сохраняет возможность offline/dev-тестирования.", 7.92, 6.14, 4.38, 0.3, 12.5, GREEN, bold=True, align=PP_ALIGN.CENTER)

    # 6. Web app
    slide = prs.slides.add_slide(blank)
    add_header(slide, "Web-приложение родителя", "Next.js + React + TypeScript + Firebase Web SDK", 6)
    add_rect(slide, 0.58, 1.62, 6.28, 4.98, PAPER, GOLD_LIGHT)
    add_picture_crop(slide, images["web_login"], 0.83, 1.88, 5.78, 3.25)
    add_text(slide, "Отдельный рабочий интерфейс для взрослого пользователя", 0.98, 5.47, 5.5, 0.52, 17, GOLD, bold=True, align=PP_ALIGN.CENTER)
    add_text(slide, "Web-клиент не дублирует игру, а управляет семейным сценарием.", 1.12, 6.04, 5.2, 0.33, 13.5, MUTED, align=PP_ALIGN.CENTER)
    add_rect(slide, 7.16, 1.62, 5.58, 4.98, GREEN_LIGHT, GREEN)
    add_text(slide, "Реализованные функции", 7.53, 1.98, 4.7, 0.42, 21, GREEN, bold=True)
    feature_data = [
        ("01", "Семья и дети", "создание профиля и Unity-входа"),
        ("02", "Управление квестами", "шаблоны, сложность, награды"),
        ("03", "Проверка выполнения", "одобрение, отклонение, комментарий"),
        ("04", "Аналитика", "метрики по дням и категориям"),
        ("05", "Рекомендации", "совет при создании нового задания"),
    ]
    for idx, (num, title, subtitle) in enumerate(feature_data):
        y = 2.58 + idx * 0.73
        add_circle(slide, 7.56, y, 0.46, GOLD_LIGHT, GOLD)
        add_text(slide, num, 7.56, y + 0.11, 0.46, 0.2, 10, GOLD, bold=True, align=PP_ALIGN.CENTER)
        add_text(slide, title, 8.19, y, 2.12, 0.28, 14.5, INK, bold=True)
        add_text(slide, subtitle, 10.28, y, 2.02, 0.38, 11.7, MUTED)

    # 7. Lifecycle
    slide = prs.slides.add_slide(blank)
    add_header(slide, "Синхронизация заданий", "Единый жизненный цикл связывает действия родителя и ребенка", 7)
    add_picture_crop(slide, images["lifecycle"], 0.58, 1.57, 8.45, 5.5)
    add_rect(slide, 9.28, 1.57, 3.46, 5.5, PAPER, BLUE_LIGHT)
    add_text(slide, "Что синхронизируется", 9.58, 1.91, 2.83, 0.44, 20, BLUE, bold=True)
    add_bullet_list(
        slide,
        [
            "статус квеста;",
            "описание и категория;",
            "сложность и награда;",
            "комментарии ребенка и родителя;",
            "временные метки этапов;",
            "признак рекомендации.",
        ],
        9.58,
        2.56,
        2.75,
        2.62,
        15.3,
        BLUE,
        GOLD,
        gap=6,
    )
    add_rect(slide, 9.58, 5.55, 2.78, 1.03, GOLD_LIGHT, GOLD_LIGHT)
    add_text(slide, "Общие статусы исключают расхождения между C#- и TypeScript-моделями.", 9.78, 5.76, 2.38, 0.58, 12.5, INK, bold=True, align=PP_ALIGN.CENTER)

    # 8. Firestore
    slide = prs.slides.add_slide(blank)
    add_header(slide, "Изменение модели Firestore", "Базовая структура сохранена и расширена аналитическим контуром", 8)
    add_text(slide, "Базовые сущности", 0.72, 1.67, 3.1, 0.36, 18, BLUE, bold=True)
    base_nodes = [
        ("users/{uid}", "роль и профиль", BLUE_LIGHT, BLUE),
        ("families/{familyId}", "семейная группа", GREEN_LIGHT, GREEN),
        ("children/{childId}", "профиль ребенка", PURPLE_LIGHT, PURPLE),
    ]
    for idx, (title, subtitle, fill, color) in enumerate(base_nodes):
        x = 0.72 + idx * 2.32
        add_rect(slide, x, 2.12, 2.06, 1.1, fill, color)
        add_text(slide, title, x + 0.12, 2.34, 1.82, 0.28, 14, color, bold=True, align=PP_ALIGN.CENTER)
        add_text(slide, subtitle, x + 0.12, 2.72, 1.82, 0.24, 11.5, MUTED, align=PP_ALIGN.CENTER)
    add_line(slide, 5.47, 3.22, 5.47, 3.72, PURPLE, 2, arrow=True)
    add_text(slide, "Подколлекции ребенка", 0.72, 3.72, 3.4, 0.36, 18, PURPLE, bold=True)
    child_nodes = [
        ("gameState/main", "уровень, XP, золото", GOLD_LIGHT, GOLD),
        ("quests/{questId}", "квест и его статусы", BLUE_LIGHT, BLUE),
        ("rewardLedger", "учет начислений", GREEN_LIGHT, GREEN),
    ]
    for idx, (title, subtitle, fill, color) in enumerate(child_nodes):
        x = 0.72 + idx * 2.32
        add_rect(slide, x, 4.17, 2.06, 1.1, fill, color)
        add_text(slide, title, x + 0.1, 4.39, 1.86, 0.28, 13.3, color, bold=True, align=PP_ALIGN.CENTER)
        add_text(slide, subtitle, x + 0.1, 4.77, 1.86, 0.24, 11.2, MUTED, align=PP_ALIGN.CENTER)
    add_rect(slide, 0.72, 5.67, 6.7, 0.81, PAPER, GOLD_LIGHT)
    add_text(slide, "Небольшое изменение основной БД: новые поля difficulty, recommendationScore, recommended и временные метки.", 0.96, 5.89, 6.18, 0.42, 13.2, INK, bold=True, align=PP_ALIGN.CENTER)

    add_rect(slide, 7.82, 1.67, 4.92, 4.81, GREEN_LIGHT, GREEN)
    add_text(slide, "Новый аналитический контур", 8.16, 2.01, 4.23, 0.4, 21, GREEN, bold=True, align=PP_ALIGN.CENTER)
    analytic_nodes = [
        ("questEvents", "неизменяемый журнал событий"),
        ("analyticsDaily", "агрегаты по датам"),
        ("analyticsByCategory", "признаки по категориям"),
        ("recommendations", "результат expert-v1"),
    ]
    for idx, (title, subtitle) in enumerate(analytic_nodes):
        y = 2.65 + idx * 0.82
        add_rect(slide, 8.25, y, 4.05, 0.64, PAPER, GREEN)
        title_size = 10.8 if title == "analyticsByCategory" else 13.2
        add_text(slide, title, 8.47, y + 0.1, 1.78, 0.25, title_size, GREEN, bold=True)
        add_text(slide, subtitle, 10.21, y + 0.09, 1.87, 0.3, 11.2, MUTED)
    add_text(slide, "Firestore Rules ограничивают доступ по роли и familyId.", 8.25, 6.02, 4.05, 0.28, 12.8, GREEN, bold=True, align=PP_ALIGN.CENTER)

    # 9. Analytics
    slide = prs.slides.add_slide(blank)
    add_header(slide, "Система аналитики", "Событийная модель превращает действия пользователей в измеримые признаки", 9)
    stages = [
        ("1", "События", "created · viewed · submitted\napproved · rejected · claimed", BLUE_LIGHT, BLUE),
        ("2", "Агрегация", "по дням и категориям\nCloud Functions", PURPLE_LIGHT, PURPLE),
        ("3", "Показатели", "completionRate · rejectionRate\navgSubmitHours · viewToSubmitRate", GOLD_LIGHT, GOLD),
        ("4", "Интерфейс", "динамика, проблемные категории\nи рекомендации родителю", GREEN_LIGHT, GREEN),
    ]
    for idx, (num, title, body, fill, color) in enumerate(stages):
        x = 0.65 + idx * 3.13
        add_rect(slide, x, 2.02, 2.65, 2.6, fill, color)
        add_circle(slide, x + 0.18, 2.2, 0.5, color, color)
        add_text(slide, num, x + 0.18, 2.31, 0.5, 0.2, 11, WHITE, bold=True, align=PP_ALIGN.CENTER)
        add_text(slide, title, x + 0.78, 2.23, 1.6, 0.34, 19, color, bold=True)
        add_text(slide, body, x + 0.22, 2.94, 2.2, 1.12, 13.3, INK, align=PP_ALIGN.CENTER, valign=MSO_ANCHOR.MIDDLE)
        if idx < 3:
            add_line(slide, x + 2.68, 3.31, x + 3.04, 3.31, INK, 2.5, arrow=True)
    add_rect(slide, 0.78, 5.12, 11.78, 1.2, PAPER, GOLD_LIGHT)
    add_rich_text(
        slide,
        [
            ("Принцип устойчивости: ", True, GREEN),
            ("ошибка записи аналитического события не блокирует создание квеста, проверку или получение награды.", False, INK),
        ],
        1.15,
        5.42,
        11.05,
        0.5,
        16,
        PP_ALIGN.CENTER,
    )

    # 10. Expert system
    slide = prs.slides.add_slide(blank)
    add_header(slide, "Рекомендательная и экспертная система", "Объяснимый rule-based модуль версии expert-v1", 10)
    add_rect(slide, 0.62, 1.66, 4.2, 4.98, PAPER, PURPLE_LIGHT)
    add_text(slide, "Механизм вывода", 0.97, 2.02, 3.5, 0.4, 21, PURPLE, bold=True, align=PP_ALIGN.CENTER)
    add_rect(slide, 1.12, 2.72, 3.18, 0.75, BLUE_LIGHT, BLUE)
    add_text(slide, "Начальная оценка: 50", 1.28, 2.94, 2.86, 0.26, 16, BLUE, bold=True, align=PP_ALIGN.CENTER)
    add_text(slide, "+", 2.36, 3.58, 0.7, 0.4, 27, GOLD, bold=True, align=PP_ALIGN.CENTER)
    add_rect(slide, 1.12, 4.03, 3.18, 0.92, GOLD_LIGHT, GOLD)
    add_text(slide, "Сумма сработавших\nэкспертных правил", 1.28, 4.19, 2.86, 0.58, 15, GOLD, bold=True, align=PP_ALIGN.CENTER)
    add_text(slide, "↓", 2.36, 5.07, 0.7, 0.4, 25, PURPLE, bold=True, align=PP_ALIGN.CENTER)
    add_rect(slide, 1.12, 5.47, 3.18, 0.75, PURPLE_LIGHT, PURPLE)
    add_text(slide, "score ∈ [0; 100]", 1.28, 5.69, 2.86, 0.26, 16, PURPLE, bold=True, align=PP_ALIGN.CENTER)

    add_rect(slide, 5.14, 1.66, 4.04, 4.98, GREEN_LIGHT, GREEN)
    add_text(slide, "Примеры правил", 5.49, 2.02, 3.35, 0.4, 21, GREEN, bold=True, align=PP_ALIGN.CENTER)
    rules = [
        ("+25", "успешность ≥ 75%, мало отклонений", GREEN),
        ("+15", "среднее выполнение ≤ 24 часов", GREEN),
        ("−30", "частота отклонений ≥ 35%", RED),
        ("−25", "пять и более активных квестов", RED),
        ("−20", "усталость от одной категории", RED),
    ]
    for idx, (delta, rule, color) in enumerate(rules):
        y = 2.71 + idx * 0.65
        add_text(slide, delta, 5.48, y, 0.62, 0.3, 14.2, color, bold=True, align=PP_ALIGN.RIGHT)
        add_text(slide, rule, 6.24, y, 2.55, 0.34, 12.6, INK)
    add_tag(slide, "70–100  РЕКОМЕНДУЕТСЯ", 5.75, 6.02, 2.82, GREEN, WHITE)

    add_rect(slide, 9.48, 1.66, 3.25, 4.98, GOLD_LIGHT, GOLD)
    add_text(slide, "Результат для родителя", 9.78, 2.02, 2.65, 0.55, 20, GOLD, bold=True, align=PP_ALIGN.CENTER)
    outputs = [
        "оценка и метка;",
        "причины и риски;",
        "предлагаемые действия;",
        "сложность квеста;",
        "опыт и золото.",
    ]
    add_bullet_list(slide, outputs, 9.86, 2.93, 2.43, 2.1, 15, INK, GOLD, gap=7)
    add_rect(slide, 9.83, 5.42, 2.53, 0.83, PAPER, PAPER)
    add_text(slide, "Совет можно применить,\nно окончательное решение остается за родителем.", 10.02, 5.58, 2.15, 0.5, 11.8, INK, bold=True, align=PP_ALIGN.CENTER)

    # 11. Demo
    slide = prs.slides.add_slide(blank)
    add_header(slide, "Демонстрация работы приложения", "На защите здесь запускается короткое видео полного пользовательского сценария", 11)
    add_rect(slide, 0.72, 1.68, 11.9, 4.78, RGBColor(20, 26, 29), RGBColor(75, 86, 88))
    add_circle(slide, 5.88, 3.05, 1.55, GOLD, GOLD)
    triangle = slide.shapes.add_shape(MSO_SHAPE.ISOSCELES_TRIANGLE, Inches(6.43), Inches(3.43), Inches(0.52), Inches(0.64))
    triangle.rotation = 90
    triangle.fill.solid()
    triangle.fill.fore_color.rgb = WHITE
    triangle.line.color.rgb = WHITE
    add_text(slide, "ВСТАВИТЬ ВИДЕО ДЕМОНСТРАЦИИ", 3.6, 4.94, 6.13, 0.36, 18, WHITE, bold=True, align=PP_ALIGN.CENTER)
    add_text(slide, "Рекомендуемая длительность: 50–70 секунд", 4.33, 5.44, 4.68, 0.3, 13, RGBColor(194, 202, 204), align=PP_ALIGN.CENTER)
    add_tag(slide, "1  Вход ребенка", 1.22, 6.65, 2.08, BLUE_LIGHT, BLUE)
    add_tag(slide, "2  Квест из Web", 3.57, 6.65, 2.08, GOLD_LIGHT, GOLD)
    add_tag(slide, "3  Проверка", 5.92, 6.65, 1.82, PURPLE_LIGHT, PURPLE)
    add_tag(slide, "4  Награда", 8.0, 6.65, 1.82, GREEN_LIGHT, GREEN)
    add_tag(slide, "5  Аналитика", 10.08, 6.65, 2.08, PAPER, INK)

    # 12. Results
    slide = prs.slides.add_slide(blank)
    add_header(slide, "Результаты модернизации", "Сравнение исходного прототипа и итоговой версии", 12)
    headers = ["Критерий", "Исходная версия", "Итоговая версия"]
    widths = [3.0, 3.45, 5.35]
    xs = [0.72, 3.72, 7.17]
    for x, w, text in zip(xs, widths, headers):
        add_rect(slide, x, 1.72, w, 0.62, GREEN, GREEN, radius=False)
        add_text(slide, text, x + 0.12, 1.89, w - 0.24, 0.26, 14.5, WHITE, bold=True, align=PP_ALIGN.CENTER)
    rows = [
        ("Архитектура", "Один Unity-клиент", "Unity + Next.js + Firebase + Functions"),
        ("Задания", "Локальный список", "Полный цикл parent ↔ child"),
        ("Данные", "Игровое состояние", "Роли, семьи, квесты, события, агрегаты"),
        ("Интерфейс", "Функциональный MVP", "Адаптивная карта, доска, HUD и графика"),
        ("Аналитика", "Отсутствовала", "Дневные и категориальные показатели"),
        ("Рекомендации", "Отсутствовали", "Объяснимый expert-v1 и настройка наград"),
    ]
    for idx, row in enumerate(rows):
        y = 2.34 + idx * 0.68
        fill = PAPER if idx % 2 == 0 else RGBColor(240, 238, 230)
        for x, w, text, color, bold in [
            (xs[0], widths[0], row[0], INK, True),
            (xs[1], widths[1], row[1], MUTED, False),
            (xs[2], widths[2], row[2], GREEN, True),
        ]:
            add_rect(slide, x, y, w, 0.68, fill, RGBColor(220, 218, 209), radius=False)
            add_text(slide, text, x + 0.15, y + 0.15, w - 0.3, 0.35, 12.8, color, bold=bold, align=PP_ALIGN.CENTER, valign=MSO_ANCHOR.MIDDLE)
    add_rect(slide, 0.72, 6.56, 11.8, 0.36, GOLD_LIGHT, GOLD_LIGHT)
    add_text(slide, "Итог: доказана работоспособность общего семейного сценария и создана расширяемая основа продукта.", 1.12, 6.6, 11.0, 0.24, 13.2, INK, bold=True, align=PP_ALIGN.CENTER)

    # 13. Conclusion
    slide = prs.slides.add_slide(blank)
    add_picture_crop(slide, images["guild"], 0, 0, 13.333, 7.5)
    overlay = add_rect(slide, 0, 0, 13.333, 7.5, INK, radius=False)
    set_shape_alpha(overlay, 72)
    add_text(slide, "Выводы", 0.78, 0.72, 3.2, 0.55, 32, WHITE, bold=True, font=FONT_DISPLAY)
    add_rect(slide, 0.8, 1.45, 1.05, 0.07, GOLD, radius=False)
    add_bullet_list(
        slide,
        [
            "Unity-прототип преобразован в распределенную клиент-серверную систему.",
            "Реализовано взаимодействие ребенка и родителя через общий жизненный цикл квеста.",
            "Firestore расширен событиями, агрегатами и рекомендациями без разрушения базовой модели.",
            "Экспертная система формирует объяснимые советы по категории, сложности и награде.",
            "Архитектура допускает дальнейшее развитие: геолокация, повторяемые квесты и ML-модель.",
        ],
        0.86,
        1.86,
        8.12,
        3.8,
        18,
        WHITE,
        GOLD,
        gap=12,
    )
    add_rect(slide, 9.48, 1.08, 3.05, 4.98, CREAM, CREAM)
    add_text(slide, "Практическая\nзначимость", 9.78, 1.62, 2.45, 0.9, 23, GREEN, bold=True, align=PP_ALIGN.CENTER)
    add_text(slide, "Родитель получает удобный инструмент управления, а ребенок — игровую мотивацию выполнять реальные задачи.", 9.83, 2.92, 2.35, 1.55, 17, INK, align=PP_ALIGN.CENTER, valign=MSO_ANCHOR.MIDDLE)
    add_tag(slide, "ГОТОВЫЙ ПРОГРАММНЫЙ КОМПЛЕКС", 9.77, 5.12, 2.48, GOLD_LIGHT, INK)
    add_text(slide, "Спасибо за внимание", 0.82, 6.42, 5.9, 0.55, 27, WHITE, bold=True, font=FONT_DISPLAY)
    add_text(slide, "Готов ответить на вопросы", 0.86, 6.93, 4.0, 0.3, 13, GOLD_LIGHT)
    add_slide_number(slide, 13)

    prs.core_properties.title = "Презентация ВКР И. Д. Ерюшева"
    prs.core_properties.subject = "Геймифицированный ежедневник: Unity, Web, Firebase, аналитика и экспертная система"
    prs.core_properties.author = "И. Д. Ерюшев"
    prs.core_properties.comments = "Сформировано по итоговой версии ВКР и коду проекта."
    DOCS.mkdir(exist_ok=True)
    prs.save(PPTX_PATH)


SPEECH: list[tuple[str, str, str]] = [
    (
        "Слайд 1. Титульный",
        "20–25 секунд",
        "Здравствуйте, уважаемые члены государственной экзаменационной комиссии. "
        "Вашему вниманию представляется выпускная квалификационная работа на тему "
        "«Разработка мобильного приложения-ежедневника с использованием метода геймификации». "
        "В ходе работы исходный Unity-прототип был существенно модернизирован: к нему добавлены "
        "родительское web-приложение, общая облачная модель данных, аналитика и экспертная система рекомендаций.",
    ),
    (
        "Слайд 2. Проблема, цель и задачи",
        "45–55 секунд",
        "Основная проблема состоит в том, что обычный список дел редко вызывает у ребенка устойчивую мотивацию, "
        "а отдельная игра, напротив, не связана с реальными полезными действиями. Поэтому была поставлена цель "
        "развить игровой прототип до семейной клиент-серверной системы. Родитель должен иметь возможность "
        "создать задание и проверить его выполнение, ребенок — получить это задание в игровой форме, выполнить "
        "его и забрать награду. Для достижения цели были спроектированы два клиента, настроена синхронизация "
        "через Firebase, расширена модель Firestore, а также добавлены сбор событий, аналитика и рекомендации.",
    ),
    (
        "Слайд 3. Архитектурное развитие проекта",
        "45–55 секунд",
        "Исходная версия являлась локальным MVP на Unity. В ней уже присутствовали карта, бои, игровой прогресс "
        "и базовый список заданий, однако отсутствовал полноценный семейный процесс. Главным результатом "
        "модернизации стал переход от одного приложения к распределенной системе. Теперь Unity используется "
        "ребенком, web-панель — родителем, а Firebase выступает общим backend-слоем. Дополнительно появились "
        "событийная аналитика и экспертный модуль. Таким образом, изменения затронули не только внешний вид, "
        "но прежде всего архитектуру и назначение продукта.",
    ),
    (
        "Слайд 4. Общая архитектура",
        "55–65 секунд",
        "Система состоит из Unity-клиента ребенка, web-клиента родителя и облачной инфраструктуры Firebase. "
        "Unity отвечает за игровой сценарий: авторизацию ребенка, карту, бой, доску приключений, отправку "
        "квеста на проверку и получение награды. Родительское приложение реализовано на Next.js и TypeScript. "
        "В нем создаются дети и задания, выполняется проверка и просматривается аналитика. Firebase Authentication "
        "разделяет роли parent и child. Firestore хранит общие документы, а Cloud Functions используются для "
        "доверенных операций и пересчета аналитики. Unity обращается к Firebase через REST-интерфейс, что позволило "
        "сохранить существующую сервисную архитектуру клиента.",
    ),
    (
        "Слайд 5. Unity-клиент",
        "55–65 секунд",
        "Unity-клиент был переработан как с пользовательской, так и с программной точки зрения. Интерфейс "
        "адаптирован под мобильные экраны с учетом safe area. Карта стала игровым пространством с объектами "
        "локаций, панорамированием и масштабированием. Задания представлены на стилизованной доске приключений, "
        "а опыт и золото отображаются в постоянной верхней панели и анимированно начисляются после получения награды. "
        "В коде выделены слои сцен и интерфейса, сервисов, доменных моделей и доступа к данным. ServiceRegistry "
        "инициализирует AuthService, QuestService, GameStateService, RewardService и другие подсистемы. "
        "При этом сохранен offline-режим для быстрого тестирования.",
    ),
    (
        "Слайд 6. Web-приложение родителя",
        "50–60 секунд",
        "Для родителя разработано отдельное web-приложение на Next.js, React и TypeScript. Разделение интерфейсов "
        "является принципиальным: взрослому пользователю не требуется игровая сцена, ему нужен быстрый рабочий "
        "инструмент. В панели можно создать семью и профиль ребенка, сформировать данные для входа в Unity, "
        "создать квест из готового шаблона или вручную, выбрать сложность, принять автоматическую награду либо "
        "открыть расширенные настройки. Здесь же родитель видит задания, требующие проверки, может одобрить или "
        "отклонить выполнение и открыть страницу аналитики.",
    ),
    (
        "Слайд 7. Синхронизация заданий",
        "55–65 секунд",
        "Центральным механизмом системы является единый жизненный цикл задания. Родитель создает квест, и он "
        "получает статус assigned. Unity загружает документ из Firestore и показывает его ребенку. После выполнения "
        "ребенок отправляет квест на проверку, статус меняется на submitted. Родитель принимает решение: approved "
        "или rejected. При отклонении квест можно доработать и отправить повторно. После одобрения ребенок забирает "
        "награду, и статус становится claimed. Обе стороны используют одинаковые статусы, категории, награды и "
        "временные метки, поэтому C#- и TypeScript-модели остаются согласованными.",
    ),
    (
        "Слайд 8. Изменение Firestore",
        "55–65 секунд",
        "Основная модель Firestore была изменена эволюционно, без полной перестройки. Сохранились коллекции users, "
        "families и children, а внутри профиля ребенка — gameState, quests и журнал наград. В документы квестов "
        "добавлены сложность, оценка рекомендации, признак recommended и временные метки этапов. Отдельно был "
        "добавлен аналитический контур: questEvents хранит неизменяемый журнал действий; analyticsDaily содержит "
        "агрегаты по дням; analyticsByCategory — признаки по категориям; recommendations — готовые советы. "
        "Firestore Rules проверяют роль пользователя и принадлежность ребенка к семье.",
    ),
    (
        "Слайд 9. Аналитика",
        "55–65 секунд",
        "Аналитика построена на событийной модели. Web-клиент записывает события создания, одобрения, отклонения "
        "и отмены квеста. Unity записывает просмотр, отправку на проверку и получение награды. Cloud Function "
        "группирует журнал по датам и категориям и рассчитывает показатели: долю выполненных квестов, частоту "
        "отклонений, среднее время выполнения и конверсию из просмотра в отправку. Важное архитектурное решение "
        "состоит в том, что аналитика является дополнительным контуром: ошибка записи события не должна блокировать "
        "основной пользовательский сценарий.",
    ),
    (
        "Слайд 10. Рекомендательная и экспертная система",
        "65–80 секунд",
        "На основе аналитических признаков реализована объяснимая экспертная система expert-v1. Начальная оценка "
        "равна 50 баллам, после чего сработавшие правила увеличивают или уменьшают ее. Например, стабильное "
        "выполнение категории добавляет 25 баллов, быстрое выполнение — еще 15. Частые отклонения уменьшают оценку "
        "на 30 баллов, большое число активных заданий — на 25, а усталость от одной категории — на 20. Итог "
        "ограничивается диапазоном от 0 до 100 и переводится в метки recommended, neutral или not recommended. "
        "Система возвращает не только score, но и причины, риски, действия, предлагаемую сложность, опыт и золото. "
        "Родитель видит объяснение и самостоятельно принимает окончательное решение.",
    ),
    (
        "Слайд 11. Демонстрация",
        "50–70 секунд видео",
        "На видео показан полный сценарий. Сначала родитель создает задание в web-приложении. Затем ребенок "
        "авторизуется в Unity и видит этот квест на доске приключений. После выполнения задание отправляется "
        "на проверку. Родитель одобряет результат, после чего ребенок получает опыт и золото. В завершение "
        "показывается обновление аналитики и рекомендация для следующего задания.\n\n"
        "[Здесь запустить заранее вставленное видео. Во время ролика можно не говорить или кратко комментировать этапы.]",
    ),
    (
        "Слайд 12. Результаты модернизации",
        "50–60 секунд",
        "В результате исходный одноклиентский MVP превратился в связанный программный комплекс. Локальный список "
        "заданий заменен полным жизненным циклом между родителем и ребенком. База данных теперь хранит не только "
        "игровое состояние, но и роли, семейные связи, события, агрегаты и рекомендации. Интерфейс Unity стал "
        "адаптивным и визуально цельным. Появилась родительская web-панель, а также аналитико-рекомендательный "
        "контур. Проверены основные сценарии: вход ребенка, создание и синхронизация квеста, проверка, начисление "
        "награды и формирование рекомендации.",
    ),
    (
        "Слайд 13. Выводы",
        "45–55 секунд",
        "Таким образом, цель работы достигнута. Unity-прототип модернизирован до распределенной системы, в которой "
        "ребенок и родитель используют разные интерфейсы, но работают с единым процессом и общей моделью данных. "
        "Небольшое расширение Firestore позволило добавить журнал событий, аналитические витрины и рекомендации. "
        "Экспертная система обеспечивает объяснимость и помогает выбирать категорию, сложность и награду, не "
        "лишая родителя контроля. Практическая значимость заключается в создании основы реального семейного "
        "сервиса, который связывает полезные действия ребенка с игровым прогрессом. Спасибо за внимание. "
        "Готов ответить на вопросы.",
    ),
]


QUESTIONS = [
    (
        "Почему выбрана экспертная система, а не нейронная сеть?",
        "На текущем объеме данных rule-based подход надежнее и объяснимее. Родитель видит причины рекомендации, "
        "а правила можно проверить отдельно. При накоплении размеченной истории экспертный score можно объединить "
        "с деревом решений или другой ML-моделью.",
    ),
    (
        "Как защищено начисление наград?",
        "Критические операции предусмотрены через Cloud Functions с проверкой роли, familyId и текущего статуса "
        "квеста. Прямой Firestore-fallback оставлен только как MVP-механизм тестирования.",
    ),
    (
        "Почему Unity использует REST, а не Firebase Unity SDK?",
        "REST позволил быстро связать существующий клиент с тем же Firebase-проектом без перестройки сервисов. "
        "Доступ к данным изолирован в FirebaseRestClient, поэтому в дальнейшем реализацию можно заменить на SDK.",
    ),
    (
        "Что именно изменилось в Firestore?",
        "Базовые users, families, children, gameState и quests сохранены. В квест добавлены сложность и поля "
        "рекомендации, а рядом появились questEvents, analyticsDaily, analyticsByCategory и recommendations.",
    ),
    (
        "Что является главным научно-практическим результатом?",
        "Архитектурное объединение игровой геймификации, семейного web-управления и объяснимого рекомендательного "
        "контура в одном проверяемом пользовательском процессе.",
    ),
]


def build_speech_docx() -> None:
    doc = Document()
    section = doc.sections[0]
    section.top_margin = Cm(2)
    section.bottom_margin = Cm(2)
    section.left_margin = Cm(2.5)
    section.right_margin = Cm(1.8)

    styles = doc.styles
    styles["Normal"].font.name = "Times New Roman"
    styles["Normal"].font.size = Pt(14)
    styles["Title"].font.name = "Times New Roman"
    styles["Title"].font.size = Pt(20)
    styles["Heading 1"].font.name = "Times New Roman"
    styles["Heading 1"].font.size = Pt(16)
    styles["Heading 2"].font.name = "Times New Roman"
    styles["Heading 2"].font.size = Pt(14)

    title = doc.add_paragraph()
    title.alignment = WD_ALIGN_PARAGRAPH.CENTER
    run = title.add_run("ТЕКСТ ЗАЩИТЫ ВКР")
    run.bold = True
    run.font.name = "Times New Roman"
    run.font.size = Pt(20)

    subtitle = doc.add_paragraph()
    subtitle.alignment = WD_ALIGN_PARAGRAPH.CENTER
    run = subtitle.add_run("«Разработка мобильного приложения-ежедневника\nс использованием метода геймификации»")
    run.bold = True
    run.font.name = "Times New Roman"
    run.font.size = Pt(16)

    info = doc.add_paragraph()
    info.alignment = WD_ALIGN_PARAGRAPH.CENTER
    info.add_run("И. Д. Ерюшев · ориентировочная длительность 8–10 минут, включая видео")

    doc.add_paragraph(
        "Текст синхронизирован со слайдами презентации. Формулировки можно произносить свободно: "
        "главное сохранить логику переходов и выделенные результаты."
    )

    for heading, timing, body in SPEECH:
        doc.add_heading(heading, level=1)
        p = doc.add_paragraph()
        run = p.add_run(f"Время: {timing}")
        run.bold = True
        run.italic = True
        p = doc.add_paragraph(body)
        p.alignment = WD_ALIGN_PARAGRAPH.JUSTIFY
        p.paragraph_format.first_line_indent = Cm(1.25)
        p.paragraph_format.line_spacing = 1.15

    doc.add_page_break()
    doc.add_heading("Вероятные вопросы комиссии", level=1)
    for question, answer in QUESTIONS:
        p = doc.add_paragraph()
        r = p.add_run(question)
        r.bold = True
        p = doc.add_paragraph(answer)
        p.alignment = WD_ALIGN_PARAGRAPH.JUSTIFY

    doc.add_heading("Подсказка для видео", level=1)
    doc.add_paragraph(
        "Оптимальная последовательность: 1) создание квеста в Web; 2) вход ребенка в Unity; "
        "3) открытие доски приключений; 4) отправка задания; 5) одобрение родителем; "
        "6) анимация XP и золота; 7) экран аналитики и рекомендации. "
        "Видео лучше записать без пауз и системных уведомлений, в разрешении 1920×1080."
    )

    doc.save(SPEECH_PATH)

    md = [
        "# Текст защиты ВКР",
        "",
        "**Тема:** «Разработка мобильного приложения-ежедневника с использованием метода геймификации»",
        "",
        "**Докладчик:** И. Д. Ерюшев  ",
        "**Длительность:** 8–10 минут, включая видео",
        "",
    ]
    for heading, timing, body in SPEECH:
        md.extend([f"## {heading}", "", f"*Время: {timing}*", "", body, ""])
    md.extend(["## Вероятные вопросы комиссии", ""])
    for question, answer in QUESTIONS:
        md.extend([f"**{question}**", "", answer, ""])
    SPEECH_MD_PATH.write_text("\n".join(md), encoding="utf-8")


def main() -> None:
    images = prepare_images()
    missing = [str(path) for path in images.values() if not path.exists()]
    if missing:
        raise FileNotFoundError("Missing presentation assets:\n" + "\n".join(missing))
    build_presentation(images)
    build_speech_docx()
    print(PPTX_PATH)
    print(SPEECH_PATH)
    print(SPEECH_MD_PATH)


if __name__ == "__main__":
    main()
