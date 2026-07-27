#!/usr/bin/env python3
"""
Populates the running API with real customer records from the UCI Bank
Marketing dataset (HybridDecisionIntelligence.API/Data/BankData.csv), so the
dashboard shows genuine ML + business-rule decisions instead of a handful of
manually-entered test rows.

Usage (with the API already running on http://localhost:5050):
    python seed-demo-data.py [count] [--seed N]

Each row is sent through POST /api/v1/decisions/make-decision exactly as a
real user submission would be, so every resulting decision in the dashboard
was actually computed by the live ML model and rule engine, not faked.
"""
import csv
import json
import random
import sys
import urllib.request
import urllib.error

API_URL = "http://localhost:5050/api/v1/decisions/make-decision"
CSV_PATH = "HybridDecisionIntelligence.API/Data/BankData.csv"
CUSTOMER_ID_START = 1000


def parse_args():
    count = 40
    seed = 42
    start_id = CUSTOMER_ID_START
    args = sys.argv[1:]
    if args and not args[0].startswith("--"):
        count = int(args[0])
        args = args[1:]
    if "--seed" in args:
        seed = int(args[args.index("--seed") + 1])
    if "--start-id" in args:
        start_id = int(args[args.index("--start-id") + 1])
    return count, seed, start_id


def load_rows(path):
    with open(path, newline="", encoding="utf-8") as f:
        reader = csv.DictReader(f, delimiter=";")
        return list(reader)


def to_payload(row, customer_id):
    return {
        "customerId": customer_id,
        "age": int(float(row["age"])),
        "job": row["job"],
        "marital": row["marital"],
        "education": row["education"],
        "balance": float(row["balance"]),
        "housing": row["housing"],
        "loan": row["loan"],
        "default": row["default"],
        "duration": int(float(row["duration"])),
        "campaign": int(float(row["campaign"])),
        "previous": int(float(row["previous"])),
        "contact": row["contact"],
        "day": int(float(row["day"])),
        "month": row["month"],
        "pDays": int(float(row["pdays"])),
        "pOutcome": row["poutcome"],
    }


def post_decision(payload):
    data = json.dumps(payload).encode("utf-8")
    req = urllib.request.Request(
        API_URL, data=data, headers={"Content-Type": "application/json"}, method="POST"
    )
    with urllib.request.urlopen(req, timeout=15) as resp:
        return json.loads(resp.read().decode("utf-8"))


def main():
    count, seed, start_id = parse_args()
    rows = load_rows(CSV_PATH)
    rng = random.Random(seed)
    sample = rng.sample(rows, count)

    approved = 0
    rejected = 0
    failed = 0

    for i, row in enumerate(sample):
        customer_id = start_id + i
        payload = to_payload(row, customer_id)
        try:
            result = post_decision(payload)
            status = "MIRATUAR" if result["finalDecision"] else "REFUZUAR"
            if result["finalDecision"]:
                approved += 1
            else:
                rejected += 1
            print(
                f"[{i + 1}/{count}] Klienti #{customer_id}: {status} "
                f"(besueshmëria AI: {result['mlConfidence'] * 100:.1f}%, "
                f"anuluar nga rregullat: {result['wasOverridden']})"
            )
        except urllib.error.HTTPError as e:
            failed += 1
            print(f"[{i + 1}/{count}] Klienti #{customer_id}: DESHTOI - {e.read().decode('utf-8')}")
        except Exception as e:
            failed += 1
            print(f"[{i + 1}/{count}] Klienti #{customer_id}: DESHTOI - {e}")

    print(
        f"\nPërfunduar: {approved} miratuar, {rejected} refuzuar, {failed} dështuan "
        f"(nga {count} klientë realë të dataset-it UCI Bank Marketing)."
    )


if __name__ == "__main__":
    main()
