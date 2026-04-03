#!/usr/bin/env python3
"""
PDF'i Docling ile parse edip Markdown olarak stdout'a yazar.
Kullanim: python3 docling_parse.py /path/to/file.pdf
"""
import sys
import os

def main():
    if len(sys.argv) < 2:
        print("HATA: PDF dosya yolu gerekli", file=sys.stderr)
        sys.exit(1)

    pdf_path = sys.argv[1]
    if not os.path.exists(pdf_path):
        print(f"HATA: Dosya bulunamadi: {pdf_path}", file=sys.stderr)
        sys.exit(1)

    # Docling thread sayisini sinirla (performans icin)
    os.environ.setdefault("OMP_NUM_THREADS", "4")

    from docling.document_converter import DocumentConverter

    converter = DocumentConverter()
    result = converter.convert(pdf_path)
    markdown = result.document.export_to_markdown()
    print(markdown)

if __name__ == "__main__":
    main()
