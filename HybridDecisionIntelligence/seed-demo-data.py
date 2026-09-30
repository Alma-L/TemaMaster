#!/usr/bin/env python3
"""
Populates the running API with real customer records from the UCI Bank
Marketing dataset (HybridDecisionIntelligence.API/Data/BankData.csv), so the
dashboard shows genuine ML + business-rule decisions instead of a handful of
manually-entered test rows.

Usage (with the API already running on http://localhost:5050):
    python seed-demo-data.py [count] [--seed N] [--start-id N] [--api-url URL]

The sample is a simple random sample of the dataset, so approval and override
rates in the dashboard reflect the real customer population. Rare outcomes
(approvals, rule overrides) need a large count to be estimated reliably.

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
    api_url = API_URL
    args = sys.argv[1:]
    if args and not args[0].startswith("--"):
        count = int(args[0])
        args = args[1:]
    if "--seed" in args:
        seed = int(args[args.index("--seed") + 1])
    if "--start-id" in args:
        start_id = int(args[args.index("--start-id") + 1])
    if "--api-url" in args:
        api_url = args[args.index("--api-url") + 1]
    return count, seed, start_id, api_url


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


def post_decision(payload, api_url):
    data = json.dumps(payload).encode("utf-8")
    req = urllib.request.Request(
        api_url, data=data, headers={"Content-Type": "application/json"}, method="POST"
    )
    with urllib.request.urlopen(req, timeout=15) as resp:
        return json.loads(resp.read().decode("utf-8"))


def main():
    count, seed, start_id, api_url = parse_args()
    rows = load_rows(CSV_PATH)
    rng = random.Random(seed)
    sample = rng.sample(rows, min(count, len(rows)))
    count = len(sample)

    approved = 0
    rejected = 0
    overridden = 0
    failed = 0

    for i, row in enumerate(sample):
        customer_id = start_id + i
        payload = to_payload(row, customer_id)
        try:
            result = post_decision(payload, api_url)
            status = "MIRATUAR" if result["finalDecision"] else "REFUZUAR"
            if result["finalDecision"]:
                approved += 1
            else:
                rejected += 1
            if result["wasOverridden"]:
                overridden += 1
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
        f"\nPërfunduar: {approved} miratuar, {rejected} refuzuar "
        f"({overridden} anuluar nga rregullat), {failed} dështuan "
        f"(nga {count} klientë realë të dataset-it UCI Bank Marketing)."
    )


if __name__ == "__main__":
    main()
