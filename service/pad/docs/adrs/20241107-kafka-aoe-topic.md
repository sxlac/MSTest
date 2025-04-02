# Create a new topic for PAD AoE symptom answers to be published to

- Status: approved
- Date: 2024-11-07
- Tags: infrastructure

## Context and Problem Statement

PAD is now collecting AoE (Atherosclerosis of Extremities) symptom answers to support diagnostics. These answers are collected independently of whether an exam is performed or not. The business is requesting that these answers are published into the data marts for all finalized evaluations that were flagged for PAD, whether PAD was performed or not performed.

PAD currently published messages via Kafka to the status topic and the results topic.

## Considered Options

- Publishing the AoE answers to the status topic did not make sense because these answers are not a status like "performed", "billrequestsent", or "notperformed".
- Publishing the AoE answers to the results topic is more closely aligned than the status topic but these are not the results of a medical test. These are supporting data thus it would not be appropriate to publish them alongside results. Additionally, results are only published for "performed" evaluations and we need to publish for "notperformed" as well.

## Decision Outcome

Through design discussions with DPS and DataMart teams, we have decided to create a new topic for publishing clinical support information such as the AoE symptoms answer.