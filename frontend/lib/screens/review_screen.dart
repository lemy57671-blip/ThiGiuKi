import 'package:flutter/material.dart';
import '../models/reading_models.dart';
import '../services/reading_api.dart';
import '../widgets/mochi_theme.dart';

class ReviewScreen extends StatefulWidget {
  const ReviewScreen({super.key, required this.api, required this.attemptId});
  final ReadingApi api;
  final String attemptId;
  @override
  State<ReviewScreen> createState() => _ReviewScreenState();
}

class _ReviewScreenState extends State<ReviewScreen> {
  late Future<ReviewInfo> data = widget.api.review(widget.attemptId);
  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('Xem lại đáp án & giải thích')),
    body: FutureBuilder<ReviewInfo>(
      future: data,
      builder: (context, snapshot) {
        if (snapshot.hasError) {
          return ReadingError(
            snapshot.error!,
            retry: () =>
                setState(() => data = widget.api.review(widget.attemptId)),
          );
        }
        if (!snapshot.hasData) {
          return const Center(child: CircularProgressIndicator());
        }
        final review = snapshot.data!;
        return MobilePage(
          child: ListView.builder(
            padding: const EdgeInsets.all(16),
            itemCount: review.paper.questions.length,
            itemBuilder: (context, index) {
              final q = review.paper.questions[index];
              final result = review.questions.singleWhere(
                (r) => r.questionId == q.id,
              );
              final group = q.groupId == null
                  ? null
                  : review.paper.groups.singleWhere((g) => g.id == q.groupId);
              return Padding(
                padding: const EdgeInsets.only(bottom: 20),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    if (group != null &&
                        (index == 0 ||
                            review.paper.questions[index - 1].groupId !=
                                q.groupId)) ...[
                      MochiCard(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.stretch,
                          children: [
                            Pill(
                              'Bài đọc ${group.first}–${group.last} · ${group.blocks.length} tài liệu',
                              color: Mochi.mint,
                            ),
                            const SizedBox(height: 12),
                            for (final block in group.blocks)
                              Padding(
                                padding: const EdgeInsets.only(bottom: 16),
                                child: block.bytes != null
                                    ? Image.memory(
                                        block.bytes!,
                                        fit: BoxFit.fitWidth,
                                      )
                                    : SelectableText(
                                        block.text ?? '',
                                        style: const TextStyle(
                                          fontFamily: 'Inter',
                                          height: 1.8,
                                        ),
                                      ),
                              ),
                          ],
                        ),
                      ),
                      const SizedBox(height: 16),
                    ],
                    MochiCard(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Wrap(
                            spacing: 12,
                            runSpacing: 8,
                            children: [
                              Text(
                                'Question ${q.number}',
                                style: const TextStyle(
                                  fontSize: 18,
                                  fontWeight: FontWeight.w600,
                                ),
                              ),
                              Pill(
                                result.outcome == 'CORRECT'
                                    ? 'Trả lời đúng'
                                    : result.outcome == 'WRONG'
                                    ? 'Trả lời sai'
                                    : 'Bỏ qua',
                                color: result.outcome == 'CORRECT'
                                    ? const Color(0xffecfaf5)
                                    : result.outcome == 'WRONG'
                                    ? const Color(0xffffe8f0)
                                    : Mochi.lavender,
                                foreground: result.outcome == 'CORRECT'
                                    ? Mochi.correct
                                    : result.outcome == 'WRONG'
                                    ? Mochi.wrong
                                    : Mochi.muted,
                              ),
                            ],
                          ),
                          const SizedBox(height: 14),
                          SelectableText(
                            q.stem.isEmpty
                                ? 'Chỗ trống [${q.gap ?? q.number}]'
                                : q.stem,
                            style: const TextStyle(
                              fontFamily: 'Inter',
                              fontSize: 16,
                              height: 1.7,
                            ),
                          ),
                          const SizedBox(height: 12),
                          for (final option in q.options)
                            Container(
                              width: double.infinity,
                              margin: const EdgeInsets.only(bottom: 8),
                              padding: const EdgeInsets.all(12),
                              decoration: BoxDecoration(
                                color: option.id == result.correct
                                    ? const Color(0xffecfaf5)
                                    : option.id == result.selected
                                    ? const Color(0xffffe8f0)
                                    : Mochi.canvas,
                                borderRadius: BorderRadius.circular(12),
                              ),
                              child: Text(
                                '${option.label}. ${option.content}${option.id == result.correct ? ' ✓' : ''}${option.id == result.selected ? ' · Bạn chọn' : ''}',
                                style: const TextStyle(
                                  fontFamily: 'Inter',
                                  height: 1.7,
                                ),
                              ),
                            ),
                          const SizedBox(height: 8),
                          const Text(
                            'Giải thích',
                            style: TextStyle(
                              fontWeight: FontWeight.w600,
                              color: Mochi.primary,
                            ),
                          ),
                          const SizedBox(height: 8),
                          SelectableText(
                            result.hint,
                            style: const TextStyle(height: 1.8),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              );
            },
          ),
        );
      },
    ),
  );
}
