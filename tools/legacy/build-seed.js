// Builds the .NET seed files from the Apps Script data, applying the exact same
// patches the front end applied at load time (Index.html, "authoritative question set").
// usage: node build-seed.js <outDir>
const fs = require('fs');
const path = require('path');
const here = __dirname;
const outDir = process.argv[2];
if (!outDir) throw new Error('usage: node build-seed.js <outDir>');

const MODEL = JSON.parse(fs.readFileSync(path.join(here, 'model.raw.json'), 'utf8'));
const EXCEL = JSON.parse(fs.readFileSync(path.join(here, 'excel-config.json'), 'utf8'));

// ---- patches copied from Index.html -------------------------------------------
const CANCELLED_QUESTION_IDS = new Set(['B01','B03','B07']);
MODEL.forEach(cat => { cat.qs = cat.qs.filter(q => !CANCELLED_QUESTION_IDS.has(q.id)); });

const scopeCategory = MODEL.find(cat => cat.code === 'ب');
const cadenceQuestion = scopeCategory && scopeCategory.qs.find(q => q.id === 'B02');
if(cadenceQuestion){
  Object.assign(cadenceQuestion,{
    q:'دورية المتابعة / الفحص',
    t:'اختيار',
    o:['بصفة مستمرة','شهري','ربع سنوي','نصف سنوي'],
    p:'',op:'',v:'',kind:'سياق',dir:'إيجابي',crit:false,w:[0,3,3],scale:[]
  });
}
if(scopeCategory){
  scopeCategory.qs.push(
    {id:'B10',q:'عمق الفحص حسب الدورية (إجراءات تحقق / فحص محدود / فحص كامل)',t:'نص',o:[],p:'',op:'',v:'',kind:'سياق',dir:'إيجابي',crit:false,w:[0,3,3],scale:[]},
    {id:'B11',q:'المدة اللازمة لإتمام المراجعة الشهرية والربع سنوية',t:'نص',o:[],p:'',op:'',v:'',kind:'سياق',dir:'إيجابي',crit:false,w:[0,3,3],scale:[]}
  );
}
const cashCategory = MODEL.find(cat => cat.code === 'ط');
if(cashCategory){
  cashCategory.qs.push(
    {id:'I09',q:'نسبة القروض والتسهيلات إلى صافي حقوق الملكية',t:'رقم',o:[],p:'I07',op:'=',v:'نعم',kind:'رقمي',dir:'سلبي',crit:false,w:[3,2,1],scale:[]},
    {id:'I10',q:'نسبة القروض والتسهيلات إلى صافي قيمة الأصول',t:'رقم',o:[],p:'I07',op:'=',v:'نعم',kind:'رقمي',dir:'سلبي',crit:false,w:[3,2,1],scale:[]}
  );
}
const assetsCategory = MODEL.find(cat => cat.code === 'ك');
if(assetsCategory){
  assetsCategory.qs.push(
    {id:'K09',q:'هل توجد دورة واضحة للإنفاق الرأسمالي؟',t:'نعم/لا',o:['نعم','لا','غير منطبق'],p:'',op:'',v:'',kind:'ثنائي',dir:'إيجابي',crit:false,w:[3,2,1],scale:[{o:'نعم',v:1},{o:'لا',v:0},{o:'غير منطبق',v:null}]},
    {id:'K10',q:'ما قيمة خطة زيادة الاستثمارات المخطط لها خلال عام؟',t:'رقم',o:[],p:'',op:'',v:'',kind:'رقمي',dir:'إيجابي',crit:false,w:[2,1,3],scale:[]}
  );
}
(function configureFlexibleProfileQuestions(){
  const flexibleIds=new Set(['A02','A14']);
  MODEL.forEach(cat=>cat.qs.forEach(q=>{
    if(!flexibleIds.has(q.id)) return;
    q.t='متعدد';
    q.allowCustom=true;
    if(q.id==='A02'){
      if(q.q==='النشاط الرئيسي') q.q='القطاعات / الأنشطة الرئيسية لدى العميل';
      const foodActivity='F&B – الأغذية والمشروبات';
      if(!q.o.includes(foodActivity)) q.o.push(foodActivity);
      if(Array.isArray(q.scale) && !q.scale.some(item=>item.o===foodActivity)) q.scale.push({o:foodActivity,v:null});
    }
  }));
})();
// --------------------------------------------------------------------------------

// ---- .NET migration decisions (not in Index.html) ------------------------------
// D1 (2026-10-20): E02 scored both answers 0, so a perfect evaluation could never reach 100%.
//     A system covering every cycle is the better control state.
(function fixFlatScales(){
  const e02 = MODEL.flatMap(c => c.qs).find(q => q.id === 'E02');
  if (!e02) throw new Error('E02 not found');
  e02.scale = [{o:'يغطي الكل',v:1},{o:'يغطي بعضها',v:0}];
})();
// --------------------------------------------------------------------------------

fs.writeFileSync(path.join(here, 'model.patched.json'), JSON.stringify(MODEL));

const map = (dict, value, what) => {
  if (!(value in dict)) throw new Error('Unknown ' + what + ': ' + JSON.stringify(value));
  return dict[value];
};
const TYPES = {'نعم/لا':'YesNo','اختيار':'Choice','متعدد':'Multi','نص':'Text','رقم':'Number','تاريخ':'Date'};
const KINDS = {'سياق':'Context','ثنائي':'Binary','متدرّج':'Graded','رقمي':'Numeric'};
const DIRS = {'إيجابي':'Positive','سلبي':'Negative'};
const OPS = {'=':'Equals','<>':'NotEquals'};
const FREQ = {'مرة واحدة':'OneTime','أسبوعي':'Weekly','كل أسبوعين':'BiWeekly','شهري':'Monthly','ربع سنوي':'Quarterly','نصف سنوي':'SemiAnnual','سنوي':'Annual','حسب الحاجة':'AsNeeded'};
const PRIORITY = {'منخفضة':'Low','متوسطة':'Medium','عالية':'High'};
const OWNER = {'فريق YELG':'YelgTeam','العميل':'Client','مشترك':'Shared'};
const BASIS = {'draft':'Draft','approved':'Approved'};
const RULE_OPS = {eq:'Equals',neq:'NotEquals',in:'In',contains:'Contains',gt:'GreaterThan',gte:'GreaterOrEqual',lt:'LessThan',answered:'Answered',notAnswered:'NotAnswered'};
const EFFECTS = {scoreOnly:'ScoreOnly',expandTask:'ExpandTask',addProcedure:'AddProcedure',recommendation:'Recommendation',optionalService:'OptionalService'};

// Equal base weights, like CATW = 100 / MODEL.length (rounded to the DB precision).
const baseWeight = Math.round(100 / MODEL.length * 10000) / 10000;
const allCodes = new Set(MODEL.flatMap(c => c.qs.map(q => q.id)));
const warnings = [];

const bank = {
  versionNo: 1,
  notes: 'Imported from the Apps Script MODEL with the 27 Aug 2026 patches.',
  categories: MODEL.map((cat, ci) => ({
    code: cat.code, title: cat.title, status: cat.status, sortOrder: ci + 1, baseWeight,
    questions: cat.qs.map((q, qi) => {
      (q.scale || []).forEach(s => { if (!q.o.includes(s.o)) warnings.push(q.id + ': scale option not in options: ' + s.o); });
      if (q.p && !allCodes.has(q.p)) warnings.push(q.id + ': parent ' + q.p + ' does not exist');
      return {
        code: q.id, text: q.q, sortOrder: qi + 1,
        answerType: map(TYPES, q.t, 'type'), kind: map(KINDS, q.kind, 'kind'), direction: map(DIRS, q.dir, 'dir'),
        isCritical: !!q.crit, allowCustomAnswer: !!q.allowCustom, isActive: q.active !== false,
        weights: q.w,
        parentCode: q.p || null,
        conditionOperator: q.p ? map(OPS, q.op || '=', 'op') : null,
        conditionValue: q.p ? q.v : null,
        options: q.o.map((o, oi) => {
          const s = (q.scale || []).find(x => x.o === o);
          return { text: o, sortOrder: oi + 1, score: s && s.v !== undefined ? s.v : null };
        })
      };
    })
  }))
};

const library = {
  templates: EXCEL.templates.map(t => ({
    code: t.code, title: t.title, serviceLine: t.serviceLine || null, evidence: t.evidence || null,
    purpose: t.purpose || null, defaultOwner: map(OWNER, t.defaultOwner || 'فريق YELG', 'owner'),
    sourceQuestionCode: t.sourceQuestionId || null,
    roleHours: { analyst: +t.roleHours.analyst || 0, teamLead: +t.roleHours.teamLead || 0, manager: +t.roleHours.manager || 0, partner: +t.roleHours.partner || 0 },
    driverQuestionCode: !t.driver || t.driver === 'fixed' ? null : t.driver,
    driverUnit: Math.max(1, +t.unit || 1), maxMultiplier: Math.max(1, +t.maxMultiplier || 4),
    frequency: map(FREQ, t.frequency || 'حسب الحاجة', 'frequency'), priority: map(PRIORITY, t.priority || 'متوسطة', 'priority'),
    defaultIncluded: !!t.defaultIncluded, isActive: t.active !== false,
    basisStatus: map(BASIS, t.basisStatus || 'draft', 'basis'), basisNote: t.basisNote || null
  })),
  rules: EXCEL.rules.map(r => {
    if (!allCodes.has(r.questionId)) warnings.push(r.id + ': question ' + r.questionId + ' does not exist');
    return {
      code: r.id, questionCode: r.questionId, operator: map(RULE_OPS, r.operator, 'rule operator'), value: r.value || null,
      effectType: map(EFFECTS, r.effectType || 'addProcedure', 'effect'),
      templateCode: r.effectType === 'expandTask' ? (r.targetTaskCode || null) : (r.templateCode || null),
      recommendation: r.recommendation || null,
      roleDelta: { analyst: +(r.roleDelta||{}).analyst || 0, teamLead: +(r.roleDelta||{}).teamLead || 0, manager: +(r.roleDelta||{}).manager || 0, partner: +(r.roleDelta||{}).partner || 0 },
      priority: map(PRIORITY, r.priority || 'متوسطة', 'priority'), conditionNote: r.conditionNote || null, isActive: r.active !== false
    };
  })
};
const templateCodes = new Set(library.templates.map(t => t.code));
library.rules.forEach(r => { if (r.templateCode && !templateCodes.has(r.templateCode)) warnings.push(r.code + ': template ' + r.templateCode + ' missing'); });

fs.mkdirSync(outDir, { recursive: true });
fs.writeFileSync(path.join(outDir, 'question-bank.json'), JSON.stringify(bank, null, 2));
fs.writeFileSync(path.join(outDir, 'task-library.json'), JSON.stringify(library, null, 2));

console.log('categories', bank.categories.length,
  'questions', bank.categories.reduce((n, c) => n + c.questions.length, 0),
  'critical', bank.categories.reduce((n, c) => n + c.questions.filter(q => q.isCritical).length, 0),
  'templates', library.templates.length, 'rules', library.rules.length);
console.log('per category', bank.categories.map(c => c.code + ':' + c.questions.length).join(' '));
console.log(warnings.length ? 'WARNINGS\n' + warnings.join('\n') : 'no warnings');
