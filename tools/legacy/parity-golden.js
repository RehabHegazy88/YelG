// Runs the ORIGINAL Index.html scoring/scope functions on fixed cases and writes golden results
// for the C# parity tests. The functions between the "verbatim" markers are copied unchanged.
// usage: node parity-golden.js <outFile>
const fs = require('fs');
const path = require('path');
const outFile = process.argv[2];
if (!outFile) throw new Error('usage: node parity-golden.js <outFile>');

const MODEL = JSON.parse(fs.readFileSync(path.join(__dirname, 'model.patched.json'), 'utf8'));
const EXCEL = JSON.parse(fs.readFileSync(path.join(__dirname, 'excel-config.json'), 'utf8'));
const ACTIVITIES = ['توزيع وتجارة','تصنيع','خدمات','خدمات طبية','مقاولات','F&B – الأغذية والمشروبات','نشاط متعدد'];

let FACTORS = {}, CATW = {};
const SCOPE_STATE = {};
const WORKLOAD_SETTINGS = { rates:{analyst:0,teamLead:0,manager:0,partner:0}, marginPct:0 };
const ROLE_KEYS=['analyst','teamLead','manager','partner'];
const TASK_ROLE_DEFAULTS = {};
const activeQs = c => c.qs.filter(q=>q.active!==false);
const all = MODEL.flatMap(activeQs);
const byId = Object.fromEntries(all.map(q=>[q.id,q]));

function resetWeights(){
  CATW = {}; FACTORS = {};
  MODEL.forEach(c => CATW[c.code] = +(100/MODEL.length).toFixed(1));
  ACTIVITIES.forEach(a => { FACTORS[a]={}; MODEL.forEach(c=>FACTORS[a][c.code]=1); });
}

// ===================== verbatim from Index.html =====================
function normalizeTaskTemplate(task){
  const fallback=TASK_ROLE_DEFAULTS[task.code]||{analyst:0,teamLead:+task.baseHours||0,manager:0,partner:0};
  task.roleHours=Object.assign({},fallback,task.roleHours||{});
  ROLE_KEYS.forEach(role=>task.roleHours[role]=Math.max(0,+task.roleHours[role]||0));
  task.baseHours=ROLE_KEYS.reduce((sum,role)=>sum+task.roleHours[role],0);
  task.basisStatus=task.basisStatus||'draft';
  task.basisNote=task.basisNote||'افتراض مبدئي — يحتاج اعتماد YELG';
  task.maxMultiplier=Math.max(1,+task.maxMultiplier||4);
  return task;
}
function isMultiQuestion(q){ return !!q && q.t==='متعدد'; }
function answerList(q,A){
  const value=A[q.id];
  if(Array.isArray(value)) return value.map(x=>String(x).trim()).filter(Boolean);
  if(value===undefined||value===null||value==='') return [];
  return [String(value).trim()].filter(Boolean);
}
function isAnswered(q,A){
  return isMultiQuestion(q) ? answerList(q,A).length>0 : A[q.id]!==undefined&&A[q.id]!==null&&A[q.id]!=='';
}
function companyScopeKey(co,index){
  return String((co&&co.id)||('company-'+index+'-'+((co&&co.name)||'unknown')));
}
function numericValue(value){
  if(Array.isArray(value)) value=value[0];
  const arabic='٠١٢٣٤٥٦٧٨٩', eastern='۰۱۲۳۴۵۶۷۸۹';
  const normalized=String(value==null?'':value)
    .replace(/[٠-٩]/g,d=>arabic.indexOf(d)).replace(/[۰-۹]/g,d=>eastern.indexOf(d))
    .replace(/,/g,'').replace(/[^0-9.\-]/g,' ');
  const match=normalized.match(/-?\d+(?:\.\d+)?/);
  return match?+match[0]:0;
}
function ruleMatches(rule,co){
  if(!rule||rule.active===false||!co) return false;
  const answer=co.answers?co.answers[rule.questionId]:undefined;
  const values=Array.isArray(answer)?answer.map(String):[String(answer==null?'':answer)];
  const expected=String(rule.value==null?'':rule.value);
  if(rule.operator==='answered') return values.some(Boolean);
  if(rule.operator==='notAnswered') return !values.some(Boolean);
  if(rule.operator==='contains') return values.some(v=>v.includes(expected));
  if(rule.operator==='in') return expected.split('|').map(x=>x.trim()).filter(Boolean).some(x=>values.includes(x));
  if(rule.operator==='neq') return !values.includes(expected);
  if(rule.operator==='gt') return numericValue(answer)>numericValue(expected);
  if(rule.operator==='gte') return numericValue(answer)>=numericValue(expected);
  if(rule.operator==='lt') return numericValue(answer)<numericValue(expected);
  return values.includes(expected);
}
function scopeRecord(co,index,taskCode,template){
  const key=companyScopeKey(co,index);
  if(!SCOPE_STATE[key]) SCOPE_STATE[key]={};
  if(!SCOPE_STATE[key][taskCode]) SCOPE_STATE[key][taskCode]={status:'planned',owner:(template&&template.defaultOwner)||'فريق YELG',dueDate:'',evidenceNote:''};
  return SCOPE_STATE[key][taskCode];
}
function emptyRoleHours(){return {analyst:0,teamLead:0,manager:0,partner:0};}
function addRoleHours(target,extra,multiplier=1){
  ROLE_KEYS.forEach(role=>target[role]=(+target[role]||0)+(+((extra||{})[role])||0)*multiplier);
  return target;
}
function roleHoursTotal(hours){return ROLE_KEYS.reduce((sum,role)=>sum+(+((hours||{})[role])||0),0);}
function frequencyMultiplier(frequency){
  return ({'أسبوعي':52,'كل أسبوعين':26,'شهري':12,'ربع سنوي':4,'نصف سنوي':2,'سنوي':1,'مرة واحدة':1,'حسب الحاجة':1})[frequency]||1;
}
function taskVolumeFactor(template,co){
  if(!template.driver||template.driver==='fixed')return {factor:1,amount:0,label:'حجم ثابت'};
  const amount=numericValue(co.answers?co.answers[template.driver]:0),unit=Math.max(1,+template.unit||1),max=Math.max(1,+template.maxMultiplier||4);
  const factor=amount>0?Math.max(1,Math.min(max,Math.ceil(amount/unit))):1;
  const question=byId[template.driver];
  return {factor,amount,label:(question?question.q:template.driver)+' · كل '+unit+' وحدة'};
}
function estimateTaskWorkload(template,co,roleDelta){
  const volume=taskVolumeFactor(template,co),roleHours=emptyRoleHours();
  addRoleHours(roleHours,template.roleHours||{},volume.factor);addRoleHours(roleHours,roleDelta||{});
  ROLE_KEYS.forEach(role=>roleHours[role]=Math.round(roleHours[role]*2)/2);
  const hours=roleHoursTotal(roleHours),annualMultiplier=frequencyMultiplier(template.frequency),annualRoleHours=emptyRoleHours();
  addRoleHours(annualRoleHours,roleHours,annualMultiplier);
  return {roleHours,hours,volumeFactor:volume.factor,volumeLabel:volume.label,annualMultiplier,annualRoleHours,annualHours:hours*annualMultiplier};
}
function ruleSource(rule){
  const question=byId[rule.questionId];
  return (question?rule.questionId+' · '+question.q:rule.questionId)+' ← '+(rule.value||'تمت الإجابة');
}
function generateCompanyScope(co,index){
  const selected=new Map(),recommendations=[],optionalServices=[],scoreEffects=[];
  TASK_TEMPLATES.filter(t=>t.active!==false&&t.defaultIncluded).forEach(t=>selected.set(t.code,{template:t,sources:['برنامج المراجعة الأساسي'],priority:t.priority||'متوسطة',roleDelta:emptyRoleHours()}));
  TASK_RULES.filter(r=>ruleMatches(r,co)).forEach(rule=>{
    const effect=rule.effectType||'addProcedure',source=ruleSource(rule);
    if(effect==='scoreOnly'){scoreEffects.push({rule,source});return;}
    if(effect==='recommendation'){
      recommendations.push({id:rule.id,text:rule.recommendation||'مراجعة الملاحظة ووضع إجراء تصحيحي.',priority:rule.priority||'متوسطة',source});return;
    }
    const template=TASK_TEMPLATES.find(t=>t.code===(effect==='expandTask'?rule.targetTaskCode:rule.templateCode)&&t.active!==false);
    if(!template)return;
    if(effect==='optionalService'){
      const record=scopeRecord(co,index,'optional:'+template.code,template);
      optionalServices.push({rule,template,record,source});
      if(!record.optionalIncluded)return;
    }
    const current=selected.get(template.code)||{template,sources:[],priority:rule.priority||template.priority||'متوسطة',roleDelta:emptyRoleHours()};
    current.sources.push((effect==='expandTask'?'توسيع إجراء قائم: ':'إجراء ناتج عن الإجابة: ')+source);
    if(effect==='expandTask')addRoleHours(current.roleDelta,rule.roleDelta||{});
    if(rule.priority==='عالية')current.priority='عالية';
    selected.set(template.code,current);
  });
  const tasks=[...selected.values()].map(item=>{
    const record=scopeRecord(co,index,item.template.code,item.template),workload=estimateTaskWorkload(item.template,co,item.roleDelta);
    return Object.assign({},item.template,workload,{priority:item.priority,sources:[...new Set(item.sources)],record,roleDelta:item.roleDelta});
  });
  return {tasks,recommendations,optionalServices,scoreEffects};
}
function visible(q,A){
  if(!q.p) return true;
  const p=byId[q.p]; if(!p || !visible(p,A)) return false;
  if(!isAnswered(p,A)) return false;
  const a=A[q.p];
  const matches=Array.isArray(a) ? a.map(String).includes(q.v) : String(a)===q.v;
  return q.op==='<>' ? !matches : matches;
}
function valueOf(q,A){
  const a=A[q.id];
  if(!isAnswered(q,A)) return null;
  if(isMultiQuestion(q)){
    const values=answerList(q,A).map(option=>(q.scale||[]).find(s=>s.o===option))
      .filter(item=>item&&item.v!==null&&item.v!==undefined).map(item=>+item.v);
    return values.length ? values.reduce((sum,value)=>sum+value,0)/values.length : null;
  }
  const e=(q.scale||[]).find(s=>s.o===a);
  return (e && e.v!==null && e.v!==undefined) ? e.v : null;
}
function score(co){
  const A=co.answers, cats=[], f=FACTORS[co.activity]||{};
  let totW=0;
  MODEL.forEach(c=>{ totW += (CATW[c.code]||0)*(f[c.code]!==undefined?f[c.code]:1); });
  let conf=0, breaches=[], answered=0, visCount=0;
  MODEL.forEach(c=>{
    let s=[0,0,0], m=[0,0,0];
    activeQs(c).forEach(q=>{
      if(!visible(q,A)) return;
      visCount++;
      if(!isAnswered(q,A)) return;
      answered++;
      const v=valueOf(q,A);
      if(v===null) return;
      for(let i=0;i<3;i++){ s[i]+=v*q.w[i]; m[i]+=q.w[i]; }
      if(q.crit && v<0.5) breaches.push(q);
    });
    const pct = i => m[i]>0 ? (s[i]/m[i]*100) : null;
    const rel = totW>0 ? (CATW[c.code]||0)*(f[c.code]!==undefined?f[c.code]:1)/totW*100 : 0;
    const p0=pct(0);
    if(p0!==null) conf += p0*rel/100;
    cats.push({code:c.code,title:c.title,conf:p0,proc:pct(1),price:pct(2),rel:rel});
  });
  const grade = breaches.length ? 'منخفضة' : (conf>=80?'عالية':conf>=60?'متوسطة':'منخفضة');
  return {cats,conf,breaches,grade,answered,visCount};
}
// =================== end verbatim ===================

// ============ intentional .NET deviations (decided 2026-10-20) ============
// D1 E02 scale fix comes through model.patched.json (build-seed.js).
// D2 Arabic separators: "٬" is a thousands separator, "٫" a decimal mark. This later declaration
//    replaces the verbatim numericValue() above (function declarations are hoisted; the last wins).
function numericValue(value){
  if(Array.isArray(value)) value=value[0];
  const arabic='٠١٢٣٤٥٦٧٨٩', eastern='۰۱۲۳۴۵۶۷۸۹';
  const normalized=String(value==null?'':value)
    .replace(/[٠-٩]/g,d=>arabic.indexOf(d)).replace(/[۰-۹]/g,d=>eastern.indexOf(d))
    .replace(/[,٬]/g,'').replace(/٫/g,'.').replace(/[^0-9.\-]/g,' ');
  const match=normalized.match(/-?\d+(?:\.\d+)?/);
  return match?+match[0]:0;
}
// D3 Scope uses only answers of active, visible questions (stale hidden answers are ignored).
function visibleAnswersOf(A){
  return Object.fromEntries(Object.entries(A).filter(([id]) => byId[id] && visible(byId[id], A)));
}
// D4 Numeric score bands exist only in .NET; no band is seeded, so scores are unchanged here.
// ===========================================================================

const TASK_TEMPLATES = EXCEL.templates.map(t => normalizeTaskTemplate(JSON.parse(JSON.stringify(t))));
const TASK_RULES = EXCEL.rules.map(r => JSON.parse(JSON.stringify(r)));

// Extra synthetic templates/rules (C# enum names) so the hour arithmetic is exercised,
// because every Excel task still has 0 hours.
const AR = {
  freq:{OneTime:'مرة واحدة',Weekly:'أسبوعي',BiWeekly:'كل أسبوعين',Monthly:'شهري',Quarterly:'ربع سنوي',SemiAnnual:'نصف سنوي',Annual:'سنوي',AsNeeded:'حسب الحاجة'},
  priority:{Low:'منخفضة',Medium:'متوسطة',High:'عالية'},
  owner:{YelgTeam:'فريق YELG',Client:'العميل',Shared:'مشترك'},
  op:{Equals:'eq',NotEquals:'neq',In:'in',Contains:'contains',GreaterThan:'gt',GreaterOrEqual:'gte',LessThan:'lt',Answered:'answered',NotAnswered:'notAnswered'},
  effect:{ScoreOnly:'scoreOnly',ExpandTask:'expandTask',AddProcedure:'addProcedure',Recommendation:'recommendation',OptionalService:'optionalService'}
};
const EN = Object.fromEntries(Object.entries(AR).map(([k,d]) => [k, Object.fromEntries(Object.entries(d).map(([a,b]) => [b,a]))]));
EN.grade = {'منخفضة':'Low','متوسطة':'Medium','عالية':'High'};
EN.owner = AR.owner && Object.fromEntries(Object.entries(AR.owner).map(([a,b]) => [b,a]));

const extraTemplates = [
  {code:'T-BASE',title:'مهمة أساسية تجريبية',defaultIncluded:true,isActive:true,defaultOwner:'YelgTeam',roleHours:{analyst:3,teamLead:2,manager:1,partner:0},driverQuestionCode:'A10',driverUnit:5000000,maxMultiplier:4,frequency:'Monthly',priority:'Medium'},
  {code:'T-EXP',title:'إجراء إضافي تجريبي',defaultIncluded:false,isActive:true,defaultOwner:'Shared',roleHours:{analyst:1.25,teamLead:0.75,manager:0.3,partner:0},driverQuestionCode:'I01',driverUnit:3,maxMultiplier:4,frequency:'Quarterly',priority:'Low'},
  {code:'T-OPT',title:'خدمة اختيارية تجريبية',defaultIncluded:false,isActive:true,defaultOwner:'Client',roleHours:{analyst:0,teamLead:4,manager:2,partner:0.5},driverQuestionCode:null,driverUnit:1,maxMultiplier:4,frequency:'OneTime',priority:'Medium'},
  {code:'T-OFF',title:'مهمة موقوفة',defaultIncluded:true,isActive:false,defaultOwner:'YelgTeam',roleHours:{analyst:9,teamLead:0,manager:0,partner:0},driverQuestionCode:null,driverUnit:1,maxMultiplier:4,frequency:'Weekly',priority:'High'}
];
const extraRules = [
  {code:'R-X1',questionCode:'C05',operator:'In',value:'فصل جزئي|لا يوجد فصل',effectType:'ExpandTask',templateCode:'T-BASE',roleDelta:{analyst:0,teamLead:2,manager:1,partner:0},priority:'High',isActive:true},
  {code:'R-X2',questionCode:'G01',operator:'GreaterThan',value:'2',effectType:'AddProcedure',templateCode:'T-EXP',priority:'Medium',isActive:true},
  {code:'R-X3',questionCode:'E04',operator:'Equals',value:'لا',effectType:'OptionalService',templateCode:'T-OPT',priority:'High',isActive:true},
  {code:'R-X4',questionCode:'C10',operator:'Equals',value:'ضعيفة',effectType:'Recommendation',recommendation:'خطة 30/60/90 يومًا للإدارة المالية.',priority:'Medium',isActive:true},
  {code:'R-X5',questionCode:'A03',operator:'Equals',value:'نعم',effectType:'ScoreOnly',priority:'Medium',isActive:true},
  {code:'R-X6',questionCode:'H13',operator:'GreaterOrEqual',value:'50',effectType:'AddProcedure',templateCode:'T-EXP',priority:'High',isActive:true},
  {code:'R-X7',questionCode:'A01',operator:'Answered',value:'',effectType:'AddProcedure',templateCode:'T-OPT',priority:'High',isActive:false},
  {code:'R-X8',questionCode:'B08',operator:'Contains',value:'2025',effectType:'AddProcedure',templateCode:'T-EXP',priority:'Low',isActive:true},
  {code:'R-X9',questionCode:'J02',operator:'NotAnswered',value:'',effectType:'Recommendation',recommendation:'',priority:'Low',isActive:true},
  {code:'R-X10',questionCode:'F14',operator:'LessThan',value:'30',effectType:'Recommendation',recommendation:'تركيز الموردين منخفض.',priority:'Low',isActive:true},
  {code:'R-X11',questionCode:'D01',operator:'NotEquals',value:'نعم',effectType:'Recommendation',recommendation:'إعداد لائحة مالية.',priority:'High',isActive:true},
  {code:'R-X12',questionCode:'A08',operator:'Answered',value:'',effectType:'ExpandTask',templateCode:'T-OFF',roleDelta:{analyst:1,teamLead:0,manager:0,partner:0},priority:'High',isActive:true}
];
extraTemplates.forEach(t => TASK_TEMPLATES.push(normalizeTaskTemplate({
  code:t.code,title:t.title,defaultIncluded:t.defaultIncluded,active:t.isActive,defaultOwner:AR.owner[t.defaultOwner],
  roleHours:Object.assign({},t.roleHours),driver:t.driverQuestionCode||'fixed',unit:t.driverUnit,maxMultiplier:t.maxMultiplier,
  frequency:AR.freq[t.frequency],priority:AR.priority[t.priority]
})));
extraRules.forEach(r => TASK_RULES.push({
  id:r.code,questionId:r.questionCode,operator:AR.op[r.operator],value:r.value,effectType:AR.effect[r.effectType],
  templateCode:r.effectType==='ExpandTask'?undefined:r.templateCode,targetTaskCode:r.effectType==='ExpandTask'?r.templateCode:undefined,
  roleDelta:r.roleDelta,recommendation:r.recommendation,priority:AR.priority[r.priority],active:r.isActive
}));

// ---------- deterministic answer generation ----------
function mulberry32(a){ return function(){ a|=0; a=a+0x6D2B79F5|0; let t=Math.imul(a^a>>>15,1|a); t=t+Math.imul(t^t>>>7,61|t)^t; return ((t^t>>>14)>>>0)/4294967296; }; }
const ARABIC_DIGITS='٠١٢٣٤٥٦٧٨٩';
function toArabicDigits(s){ return String(s).replace(/[0-9]/g,d=>ARABIC_DIGITS[+d]); }

function answerFor(q, rnd, mode){
  const scored = o => { const s=(q.scale||[]).find(x=>x.o===o); return s && s.v!=null ? s.v : null; };
  if (q.o && q.o.length){
    if (q.t==='متعدد'){
      if (mode==='best' || mode==='worst'){
        const ranked = q.o.filter(o=>scored(o)!==null).sort((a,b)=>scored(b)-scored(a));
        if (!ranked.length) return [q.o[0]];
        return [mode==='best' ? ranked[0] : ranked[ranked.length-1]];
      }
      const picks = q.o.filter(()=>rnd()<0.4);
      if (q.allowCustom && rnd()<0.5) picks.push('قيمة مخصصة '+Math.floor(rnd()*10));
      return picks;
    }
    if (mode==='best' || mode==='worst'){
      const ranked = q.o.filter(o=>scored(o)!==null).sort((a,b)=>scored(b)-scored(a));
      if (!ranked.length) return q.o[0];
      return mode==='best' ? ranked[0] : ranked[ranked.length-1];
    }
    return q.o[Math.floor(rnd()*q.o.length)];
  }
  if (q.t==='رقم' || q.kind==='رقمي'){
    const n = Math.floor(rnd()*12000000);
    const pick = rnd();
    if (pick<0.3) return toArabicDigits(n);
    if (pick<0.5) return n.toLocaleString('en-US');
    if (pick<0.6) return 'حوالي '+n+' تقريبا';
    return String(n);
  }
  if (q.t==='تاريخ') return '2025-0'+(1+Math.floor(rnd()*9))+'-15';
  return rnd()<0.5 ? 'نص حر 2025' : 'ملاحظة';
}

function buildCase(name, activityIndex, mode, seed, options={}){
  const rnd = mulberry32(seed);
  const answers = {};
  if (mode !== 'empty'){
    all.forEach(q => {
      const fill = mode==='random' ? rnd() < (options.fillRate ?? 0.85) : true;
      if (!fill) return;
      const a = answerFor(q, rnd, mode);
      if (Array.isArray(a) && !a.length) return;
      answers[q.id] = a;
    });
  }
  Object.assign(answers, options.override || {});
  return { name, activity: ACTIVITIES[activityIndex], activityId: activityIndex + 1, answers,
           catWeights: options.catWeights || null, factors: options.factors || null, optionalIncluded: options.optionalIncluded || [] };
}

const cases = [
  buildCase('empty', 0, 'empty', 1),
  buildCase('best-answers', 1, 'best', 2),
  buildCase('worst-answers', 2, 'worst', 3),
  buildCase('branching-hidden-children', 3, 'empty', 4, { override:{ A03:'لا', A04:'7', A05:'نعم', G04:'لا يوجد', G05:'لا', G06:'لا', H10:'شهري', H11:'نعم', C05:'فصل كامل' } }),
  buildCase('multi-and-custom', 5, 'empty', 5, { override:{ A02:['تصنيع','F&B – الأغذية والمشروبات','قطاع جديد'], A14:['مالية','إدارة مستحدثة'], C08:['مشتريات'], C07:'نعم', E03:['رواتب','نقدية'], E02:'يغطي بعضها' } }),
  buildCase('critical-only', 4, 'empty', 6, { override:{ C05:'لا يوجد فصل', E04:'لا', I03:'لا', M08:'نعم' } }),
  buildCase('numeric-drivers', 0, 'empty', 7, { override:{ A10:'١٢٬٥٠٠٬٠٠٠', I01:'7 خزن', G01:'٣ مخازن', H13:'55%', F14:'12', A08:'40', B08:'من 2025 إلى 2026' } }),
  buildCase('stale-hidden-answers', 3, 'empty', 11, { override:{ C01:'نعم', C03:'شخص', C10:'عالية', C11:['التقارير'], E04:'نعم', E06:'نعم', E07:'لا', G04:'لا يوجد', G05:'نعم', I07:'لا', I09:'٢٫٥', H10:'لا يوجد', H11:'نعم', C05:'فصل كامل', E02:'يغطي الكل' } }),
  buildCase('optional-included', 6, 'empty', 8, { override:{ E04:'لا', C05:'فصل جزئي', A10:'3000000' }, optionalIncluded:['T-OPT'] }),
  buildCase('weights-and-factors', 2, 'random', 9, { catWeights:{'ج':20,'هـ':5}, factors:{'ج':2,'هـ':0.5,'م':0} }),
  buildCase('zero-total-weight', 1, 'random', 10, { catWeights:Object.fromEntries(MODEL.map(c=>[c.code,0])) })
];
for (let i = 0; i < 8; i++) cases.push(buildCase('random-' + (i+1), i % ACTIVITIES.length, 'random', 1000 + i, { fillRate: 0.55 + 0.05*i }));

// ---------- run ----------
const EN_STATUS = {planned:'Planned',inProgress:'InProgress',waiting:'Waiting',review:'Review',completed:'Completed'};
const results = cases.map((c, index) => {
  resetWeights();
  if (c.catWeights) Object.assign(CATW, c.catWeights);
  if (c.factors) Object.assign(FACTORS[c.activity], c.factors);
  const co = { id:'case-'+index, name:c.name, activity:c.activity, answers:JSON.parse(JSON.stringify(c.answers)) };
  c.optionalIncluded.forEach(code => { SCOPE_STATE[co.id] = SCOPE_STATE[co.id] || {}; SCOPE_STATE[co.id]['optional:'+code] = {optionalIncluded:true}; });

  const s = score(co);
  const scope = generateCompanyScope(Object.assign({}, co, { answers: visibleAnswersOf(co.answers) }), index);
  return {
    name: c.name, activityId: c.activityId, catWeights: c.catWeights, factors: c.factors,
    optionalIncluded: c.optionalIncluded,
    answers: Object.fromEntries(Object.entries(c.answers).map(([k,v]) => [k, Array.isArray(v) ? v : [String(v)]])),
    expected: {
      score: {
        confidence: s.conf, grade: EN.grade[s.grade], answered: s.answered, visibleCount: s.visCount,
        breaches: s.breaches.map(q => q.id),
        categories: s.cats.map(x => ({ code:x.code, confidence:x.conf, workflow:x.proc, pricing:x.price, relativeWeight:x.rel }))
      },
      scope: {
        tasks: scope.tasks.map(t => ({
          code:t.code, priority:EN.priority[t.priority], sources:t.sources, roleHours:t.roleHours, hours:t.hours,
          volumeFactor:t.volumeFactor, volumeLabel:t.volumeLabel, annualMultiplier:t.annualMultiplier, annualHours:t.annualHours,
          owner:EN.owner[t.record.owner], status:EN_STATUS[t.record.status]
        })),
        recommendations: scope.recommendations.map(r => ({ code:r.id, text:r.text, priority:EN.priority[r.priority], source:r.source })),
        optionalServices: scope.optionalServices.map(o => ({ ruleCode:o.rule.id, templateCode:o.template.code, included:!!o.record.optionalIncluded, source:o.source })),
        scoreEffects: scope.scoreEffects.map(e => e.rule.id)
      }
    }
  };
});

fs.mkdirSync(path.dirname(outFile), { recursive: true });
fs.writeFileSync(outFile, JSON.stringify({ extraTemplates, extraRules, cases: results }, null, 1));
console.log('cases', results.length);
results.forEach(r => console.log(r.name.padEnd(28), 'conf', r.expected.score.confidence.toFixed(3).padStart(8), r.expected.score.grade.padEnd(6),
  'ans', String(r.expected.score.answered).padStart(3)+'/'+String(r.expected.score.visibleCount).padEnd(3),
  'breaches', r.expected.score.breaches.length, 'tasks', r.expected.scope.tasks.length, 'recs', r.expected.scope.recommendations.length));
