import 'dart:convert';
import 'dart:typed_data';

typedef Json = Map<String, dynamic>;

class ExamInfo {
  ExamInfo.fromJson(Json j)
    : id = j['id'] as int,
      title = j['title'] as String,
      sourceId = j['sourceId'] as String,
      version = j['version'] as String,
      status = j['status'] as String,
      minutes = j['durationMinutes'] as int,
      count = j['questionCount'] as int;
  final int id, minutes, count;
  final String title, sourceId, version, status;
}

class OptionInfo {
  OptionInfo.fromJson(Json j)
    : id = j['id'] as int,
      label = j['label'] as String,
      content = j['content'] as String;
  final int id;
  final String label, content;
}

class QuestionInfo {
  QuestionInfo.fromJson(Json j)
    : id = j['id'] as int,
      number = j['originalNumber'] as int,
      order = j['displayOrder'] as int,
      part = j['partNumber'] as int,
      groupId = j['groupId'] as int?,
      gap = j['gapLabel'] as int?,
      stem = j['stem'] as String,
      options = (j['options'] as List)
          .map((e) => OptionInfo.fromJson(e as Json))
          .toList();
  final int id, number, order, part;
  final int? groupId, gap;
  final String stem;
  final List<OptionInfo> options;
}

class BlockInfo {
  BlockInfo.fromJson(Json j)
    : id = j['id'] as int,
      type = j['blockType'] as String,
      text = j['textContent'] as String?,
      bytes = j['imageBase64'] == null
          ? null
          : base64Decode(j['imageBase64'] as String);
  final int id;
  final String type;
  final String? text;
  final Uint8List? bytes;
}

class GroupInfo {
  GroupInfo.fromJson(Json j)
    : id = j['id'] as int,
      first = j['firstQuestionNumber'] as int,
      last = j['lastQuestionNumber'] as int,
      blocks = (j['blocks'] as List)
          .map((e) => BlockInfo.fromJson(e as Json))
          .toList();
  final int id, first, last;
  final List<BlockInfo> blocks;
}

class PaperInfo {
  PaperInfo.fromJson(Json j)
    : exam = ExamInfo.fromJson(j['exam'] as Json),
      questions = (j['questions'] as List)
          .map((e) => QuestionInfo.fromJson(e as Json))
          .toList(),
      groups = (j['groups'] as List)
          .map((e) => GroupInfo.fromJson(e as Json))
          .toList();
  final ExamInfo exam;
  final List<QuestionInfo> questions;
  final List<GroupInfo> groups;
}

class AnswerInfo {
  AnswerInfo.fromJson(Json j)
    : questionId = j['questionId'] as int,
      optionId = j['selectedOptionId'] as int?,
      rowVersion = j['rowVersion'] as String;
  final int questionId;
  final int? optionId;
  final String rowVersion;
}

class AttemptInfo {
  AttemptInfo.fromJson(Json j)
    : id = j['id'] as String,
      examId = j['examId'] as int,
      title = j['examTitle'] as String,
      status = j['status'] as String,
      started = DateTime.parse(j['startedAtUtc'] as String).toUtc(),
      expires = DateTime.parse(j['expiresAtUtc'] as String).toUtc(),
      serverNow = DateTime.parse(j['serverNowUtc'] as String).toUtc(),
      total = j['totalQuestions'] as int,
      correct = j['correctCount'] as int?,
      wrong = j['wrongCount'] as int?,
      skipped = j['skippedCount'] as int?,
      accuracy = (j['accuracyPercent'] as num?)?.toDouble(),
      partResults = ((j['partResults'] as List?) ?? [])
          .map((p) => PartResultInfo.fromJson(p as Json))
          .toList(),
      answers = (j['answers'] as List)
          .map((e) => AnswerInfo.fromJson(e as Json))
          .toList();
  final String id, title, status;
  final int examId, total;
  final int? correct, wrong, skipped;
  final double? accuracy;
  final DateTime started, expires, serverNow;
  final List<AnswerInfo> answers;
  final List<PartResultInfo> partResults;
  bool get finished => status != 'IN_PROGRESS';
  int get answered => answers.where((a) => a.optionId != null).length;
}

class PartResultInfo {
  PartResultInfo.fromJson(Json j)
    : part = j['partNumber'] as int,
      total = j['total'] as int,
      correct = j['correct'] as int,
      wrong = j['wrong'] as int,
      skipped = j['skipped'] as int;
  final int part, total, correct, wrong, skipped;
}

class QuestionReviewInfo {
  QuestionReviewInfo.fromJson(Json j)
    : questionId = j['questionId'] as int,
      selected = j['selectedOptionId'] as int?,
      correct = j['correctOptionId'] as int,
      hint = j['hint'] as String,
      outcome = j['outcome'] as String;
  final int questionId, correct;
  final int? selected;
  final String hint, outcome;
}

class ReviewInfo {
  ReviewInfo.fromJson(Json j)
    : paper = PaperInfo.fromJson(j['paper'] as Json),
      questions = (j['questions'] as List)
          .map((q) => QuestionReviewInfo.fromJson(q as Json))
          .toList();
  final PaperInfo paper;
  final List<QuestionReviewInfo> questions;
}
