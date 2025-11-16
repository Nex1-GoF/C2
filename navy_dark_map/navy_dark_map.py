from fastapi import FastAPI, Response
import httpx
from PIL import Image
from io import BytesIO
import os
import numpy as np

app = FastAPI()

# 캐시 디렉토리
TILE_CACHE_DIR = "./cache"
os.makedirs(TILE_CACHE_DIR, exist_ok=True)

# OSM 기본 타일 URL
OSM_BASE = "https://tile.openstreetmap.org/{z}/{y}/{x}.png"

# HTTP Client 재사용 → 속도 상승
client = httpx.AsyncClient(
    headers={"User-Agent": "NavyDarkTileServer/1.0"},
    timeout=10
)


# ============================================
#   핵심 네온-사이안 라인맵 필터 (최종본)
# ============================================
def recolor_to_navy_dark(img: Image.Image) -> Image.Image:
    img = img.convert("RGB")
    arr = np.array(img).astype(np.float32)

    r = arr[:, :, 0]
    g = arr[:, :, 1]
    b = arr[:, :, 2]

    brightness = (r + g + b) / 3.0

    # ------------------------------
    # 1) 전체 네이비 톤으로 이동
    # ------------------------------
    navy_r, navy_g, navy_b = 5, 15, 40
    arr[:, :, 0] = r * 0.15 + navy_r
    arr[:, :, 1] = g * 0.20 + navy_g
    arr[:, :, 2] = b * 0.25 + navy_b

    # ------------------------------
    # 2) 밝은 요소(도로/경계선) 사이안으로 강조
    # ------------------------------
    mask = brightness > 120

    arr[mask, 0] = 20
    arr[mask, 1] = np.clip(120 + g[mask] * 0.6, 0, 255)
    arr[mask, 2] = np.clip(200 + b[mask] * 0.8, 0, 255)

    # ------------------------------
    # 3) Glow (네온 발광 느낌)
    # ------------------------------
    glow = np.clip((brightness - 120) * 0.6, 0, 80)
    arr[:, :, 0] = np.clip(arr[:, :, 0] + glow * 0.1, 0, 255)
    arr[:, :, 1] = np.clip(arr[:, :, 1] + glow * 0.6, 0, 255)
    arr[:, :, 2] = np.clip(arr[:, :, 2] + glow * 1.0, 0, 255)

    # ------------------------------
    # 4) 텍스트, 건물 → 흰색 글자 흐림 처리
    # ------------------------------
    text_mask = brightness > 170
    arr[text_mask] = arr[text_mask] * 0.55

    # ------------------------------
    # 5) 지형/산/호수 등 적당히 보임
    # ------------------------------
    terrain_mask = (brightness > 60) & (brightness < 120)
    arr[terrain_mask] = np.clip(arr[terrain_mask] * 1.10, 0, 255)

    # ------------------------------
    # 6) 라인 및 지도 디테일 강조
    # ------------------------------
    arr = np.clip(arr * 1.08, 0, 255)

    return Image.fromarray(arr.astype(np.uint8), "RGB")


# ============================================
#   타일 처리 엔드포인트
# ============================================
@app.get("/tiles/{z}/{x}/{y}.png")
async def get_tile(z: int, x: int, y: int):
    cache_path = f"{TILE_CACHE_DIR}/{z}_{x}_{y}.png"

    # 1) 캐시 있으면 바로 반환
    if os.path.exists(cache_path):
        with open(cache_path, "rb") as f:
            return Response(content=f.read(), media_type="image/png")

    # 2) OSM 타일 다운로드
    url = OSM_BASE.format(z=z, x=x, y=y)
    resp = await client.get(url)

    if resp.status_code != 200:
        return Response(status_code=404)

    # 3) 이미지 변환 (네이비–사이안 스타일)
    try:
        img = Image.open(BytesIO(resp.content))
    except Exception:
        return Response(status_code=500)

    dark_img = recolor_to_navy_dark(img)

    # 4) 캐시 저장
    buf = BytesIO()
    dark_img.save(buf, format="PNG")
    data = buf.getvalue()

    with open(cache_path, "wb") as f:
        f.write(data)

    return Response(content=data, media_type="image/png")
