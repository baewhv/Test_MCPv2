#!/usr/bin/env python3
"""
tools/remove_bg.py
2D 게임 스프라이트 배경 투명화(Alpha Masking) 및 여백 정리 도구

용도:
- 생성형 AI(generate_image)로 생성된 단색 배경(검정, 흰색 등) 스프라이트의 배경을 투명 PNG(RGBA)로 변환
- 외곽 Flood-Fill 알고리즘을 사용하여 스프라이트 내부의 검정/흰색 픽셀(눈, 무늬 등)을 안전하게 보존

사용법:
  # 1. 단일 파일 자동 배경 투명화 (외곽 Flood Fill, 모서리 색상 자동 감지)
  python tools/remove_bg.py input.png -o output.png

  # 2. 배경색 명시 및 허용 오차 설정
  python tools/remove_bg.py input.png -o output.png --bg black --tolerance 20

  # 3. 투명 영역 기준 스프라이트 본체 자동 여백 크롭
  python tools/remove_bg.py input.png -o output.png --crop

  # 4. 폴더 내 모든 PNG 파일 일괄 변환
  python tools/remove_bg.py Assets/_Imports/Sprites/Raw/ -o Assets/_Imports/Sprites/Processed/
"""

import os
import sys
import argparse
from collections import deque
from PIL import Image


def parse_color(color_str):
    """색상 문자열('black', 'white', '#000000', '255,255,255')을 RGB 튜플로 변환"""
    if not color_str or color_str.lower() == 'auto':
        return None
    c = color_str.lower().strip()
    if c == 'black':
        return (0, 0, 0)
    elif c == 'white':
        return (255, 255, 255)
    elif c.startswith('#'):
        hex_val = c.lstrip('#')
        if len(hex_val) == 6:
            return tuple(int(hex_val[i:i+2], 16) for i in (0, 2, 4))
        elif len(hex_val) == 3:
            return tuple(int(hex_val[i]*2, 16) for i in range(3))
    elif ',' in c:
        parts = [int(p.strip()) for p in c.split(',')]
        if len(parts) >= 3:
            return tuple(parts[:3])
    raise ValueError(f"지원하지 않는 색상 형식입니다: {color_str}")


def color_distance(c1, c2):
    """두 RGB 색상 간의 유클리드 거리 계산"""
    return ((c1[0] - c2[0]) ** 2 + (c1[1] - c2[1]) ** 2 + (c1[2] - c2[2]) ** 2) ** 0.5


def sample_corner_background_color(img):
    """이미지 네 모서리 픽셀을 샘플링하여 가장 지배적인 배경색 감지"""
    w, h = img.size
    corners = [
        img.getpixel((0, 0))[:3],
        img.getpixel((w - 1, 0))[:3],
        img.getpixel((0, h - 1))[:3],
        img.getpixel((w - 1, h - 1))[:3]
    ]
    # 모서리 색상 중 가장 빈도 높은 색상 선택
    color_counts = {}
    for c in corners:
        color_counts[c] = color_counts.get(c, 0) + 1
    sorted_colors = sorted(color_counts.items(), key=lambda x: x[1], reverse=True)
    return sorted_colors[0][0]


def remove_background_floodfill(img, bg_color=None, tolerance=25, mode='floodfill'):
    """
    Flood Fill(BFS) 방식으로 외곽 배경을 감지하여 투명(Alpha 0)으로 변환
    - mode='floodfill': 외곽 테두리에서 시작하여 연결된 배경만 투명화 (내부 픽셀 보존)
    - mode='all': 이미지 전체에서 일치하는 모든 색상을 투명화 (크로마키 모드)
    """
    img = img.convert('RGBA')
    w, h = img.size
    pixels = img.load()

    if bg_color is None:
        bg_color = sample_corner_background_color(img)

    if mode == 'all':
        # 전역 색상 치환 (크로마키)
        for y in range(h):
            for x in range(w):
                r, g, b, a = pixels[x, y]
                if color_distance((r, g, b), bg_color) <= tolerance:
                    pixels[x, y] = (0, 0, 0, 0)
        return img

    # 외곽 Flood Fill (BFS)
    visited = [[False] * h for _ in range(w)]
    queue = deque()

    # 이미지의 4변 테두리 픽셀을 시작 큐에 추가
    for x in range(w):
        for y in [0, h - 1]:
            r, g, b, a = pixels[x, y]
            if color_distance((r, g, b), bg_color) <= tolerance:
                queue.append((x, y))
                visited[x][y] = True

    for y in range(h):
        for x in [0, w - 1]:
            if not visited[x][y]:
                r, g, b, a = pixels[x, y]
                if color_distance((r, g, b), bg_color) <= tolerance:
                    queue.append((x, y))
                    visited[x][y] = True

    directions = [(-1, 0), (1, 0), (0, -1), (0, 1)]

    while queue:
        cx, cy = queue.popleft()
        pixels[cx, cy] = (0, 0, 0, 0)

        for dx, dy in directions:
            nx, ny = cx + dx, cy + dy
            if 0 <= nx < w and 0 <= ny < h and not visited[nx][ny]:
                r, g, b, a = pixels[nx, ny]
                if color_distance((r, g, b), bg_color) <= tolerance:
                    visited[nx][ny] = True
                    queue.append((nx, ny))

    return img


def autocrop_transparent(img, padding=0):
    """투명 알파 채널을 기준으로 스프라이트 본체만 최소 크기로 자르기"""
    bbox = img.getbbox()
    if bbox:
        if padding > 0:
            w, h = img.size
            min_x = max(0, bbox[0] - padding)
            min_y = max(0, bbox[1] - padding)
            max_x = min(w, bbox[2] + padding)
            max_y = min(h, bbox[3] + padding)
            bbox = (min_x, min_y, max_x, max_y)
        return img.crop(bbox)
    return img


def process_image_file(input_path, output_path, bg_color=None, tolerance=25, mode='floodfill', crop=False, padding=0):
    """단일 이미지 파일 처리 및 저장"""
    try:
        with Image.open(input_path) as img:
            result_img = remove_background_floodfill(img, bg_color=bg_color, tolerance=tolerance, mode=mode)
            if crop:
                result_img = autocrop_transparent(result_img, padding=padding)

            out_dir = os.path.dirname(output_path)
            if out_dir and not os.path.exists(out_dir):
                os.makedirs(out_dir, exist_ok=True)

            result_img.save(output_path, "PNG")
            print(f"[OK] Background Removed: {input_path} -> {output_path}")
            return True
    except Exception as e:
        print(f"[ERROR] Failed to process {input_path}: {e}", file=sys.stderr)
        return False


def main():
    parser = argparse.ArgumentParser(description="2D 게임 스프라이트 배경 투명화 및 여백 정리 도구")
    parser.add_argument("input", help="입력 이미지 경로 또는 디렉토리")
    parser.add_argument("-o", "--output", help="출력 이미지 경로 또는 디렉토리 (지정하지 않으면 [파일명]_transparent.png로 저장)")
    parser.add_argument("--bg", "--bg-color", default="auto", help="배경색 (auto, black, white, #000000 등, 기본값: auto)")
    parser.add_argument("-t", "--tolerance", type=float, default=25.0, help="배경색 판별 오차 허용치 (기본값: 25.0)")
    parser.add_argument("-m", "--mode", choices=["floodfill", "all"], default="floodfill", help="제거 모드: floodfill(외곽 배경만), all(전체 일치 색상)")
    parser.add_argument("--crop", action="store_true", help="스프라이트 본체 기준 자동 여백 크롭")
    parser.add_argument("--padding", type=int, default=0, help="크롭 시 유지할 여백 픽셀 수 (기본값: 0)")

    args = parser.parse_args()

    bg_color = parse_color(args.bg)

    if os.path.isdir(args.input):
        input_dir = args.input
        output_dir = args.output if args.output else input_dir
        os.makedirs(output_dir, exist_ok=True)

        supported_exts = ('.png', '.jpg', '.jpeg', '.bmp', '.webp')
        files = [f for f in os.listdir(input_dir) if os.path.splitext(f)[1].lower() in supported_exts]

        if not files:
            print(f"디렉토리 내에 처리할 이미지 파일이 없습니다: {input_dir}")
            return

        success_count = 0
        for f in files:
            in_file = os.path.join(input_dir, f)
            out_filename = os.path.splitext(f)[0] + ".png"
            out_file = os.path.join(output_dir, out_filename)
            if process_image_file(in_file, out_file, bg_color, args.tolerance, args.mode, args.crop, args.padding):
                success_count += 1

        print(f"\n[완료] 총 {len(files)}개 중 {success_count}개 파일 배경 투명화 완료.")
    else:
        in_file = args.input
        if not os.path.exists(in_file):
            print(f"입력 파일이 존재하지 않습니다: {in_file}", file=sys.stderr)
            sys.exit(1)

        if args.output:
            out_file = args.output
        else:
            base, ext = os.path.splitext(in_file)
            out_file = f"{base}_transparent.png"

        success = process_image_file(in_file, out_file, bg_color, args.tolerance, args.mode, args.crop, args.padding)
        if not success:
            sys.exit(1)


if __name__ == "__main__":
    main()
