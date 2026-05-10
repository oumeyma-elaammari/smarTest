package com.smartest.backend.controller;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.smartest.backend.dto.request.PublicationWebRequest;
import com.smartest.backend.dto.request.ReponseQuizDTO;
import com.smartest.backend.dto.request.SoumissionQuizRequest;
import com.smartest.backend.dto.request.SoumissionQuizWebRequest;
import com.smartest.backend.dto.request.VerificationQuestionWebRequest;
import com.smartest.backend.dto.response.QuizPassageWebResponse;
import com.smartest.backend.dto.response.QuizResponse;
import com.smartest.backend.dto.response.ResultatQuizResponse;
import com.smartest.backend.dto.response.ResultatQuizWebResponse;
import com.smartest.backend.dto.response.VerificationQuestionWebResponse;
import com.smartest.backend.entity.enumeration.StatutQuiz;
import com.smartest.backend.exception.GlobalExceptionHandler;
import com.smartest.backend.service.QuizService;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;
import org.springframework.http.MediaType;
import org.springframework.security.core.authority.SimpleGrantedAuthority;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.security.core.userdetails.User;
import org.springframework.security.core.userdetails.UserDetails;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.test.web.servlet.setup.MockMvcBuilders;

import java.util.List;
import java.util.Map;

import static com.smartest.backend.testsupport.MockMvcAuthenticationSupport.authenticationPrincipalUserDetailsResolver;
import static com.smartest.backend.testsupport.MockMvcAuthenticationSupport.principal;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.ArgumentMatchers.eq;
import static org.mockito.Mockito.doNothing;
import static org.mockito.Mockito.verify;
import static org.mockito.Mockito.when;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.delete;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.patch;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.content;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

/**
 * Endpoints {@link QuizController} non couverts par {@link QuizControllerTest}
 * ni {@link QuizControllerQrEndpointsTest} (publication, passage web, soumissions).
 */
@ExtendWith(MockitoExtension.class)
class QuizControllerExtendedEndpointsTest {

    private static final String EMAIL_PROF_SCHOOL = "prof@school.com";
    private static final String EMAIL_STUDENT_SCHOOL = "student@school.com";
    private static final String JSON_PATH_SCORE = "$.score";

    private MockMvc mockMvc;
    private final ObjectMapper objectMapper = new ObjectMapper();

    @Mock
    private QuizService quizService;

    @InjectMocks
    private QuizController quizController;

    private QuizResponse quizResponse;
    private UserDetails profUser;
    private UserDetails studentUser;

    @BeforeEach
    void setUp() {
        mockMvc = MockMvcBuilders.standaloneSetup(quizController)
                .setCustomArgumentResolvers(authenticationPrincipalUserDetailsResolver())
                .setControllerAdvice(new GlobalExceptionHandler())
                .build();

        quizResponse = new QuizResponse();
        quizResponse.setId(10L);
        quizResponse.setTitre("Quiz");
        quizResponse.setStatut(StatutQuiz.PUBLIE);

        profUser = new User(EMAIL_PROF_SCHOOL, "pw", List.of(new SimpleGrantedAuthority("ROLE_PROFESSEUR")));
        studentUser = new User(EMAIL_STUDENT_SCHOOL, "pw", List.of(new SimpleGrantedAuthority("ROLE_ETUDIANT")));
    }

    @AfterEach
    void tearDown() {
        SecurityContextHolder.clearContext();
    }

    @Test
    void addQuestionToQuizReturns200() throws Exception {
        when(quizService.addQuestionToQuiz(10L, 5L)).thenReturn(quizResponse);

        mockMvc.perform(post("/api/quizs/10/questions/5"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.id").value(10));

        verify(quizService).addQuestionToQuiz(10L, 5L);
    }

    @Test
    void removeQuestionFromQuizReturns200() throws Exception {
        doNothing().when(quizService).removeQuestionFromQuiz(10L, 5L);

        mockMvc.perform(delete("/api/quizs/10/questions/5"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.message").value("Question supprimée du quiz"))
                .andExpect(jsonPath("$.success").value(true));
    }

    @Test
    void getQuizPubliesReturns200() throws Exception {
        when(quizService.getQuizPublies()).thenReturn(List.of(quizResponse));

        mockMvc.perform(get("/api/quizs/publies"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.length()").value(1))
                .andExpect(jsonPath("$[0].id").value(10));
    }

    @Test
    void getMesPublicationsWebPassesStudentEmailToService() throws Exception {
        when(quizService.getMesQuizsPublicationWeb(EMAIL_STUDENT_SCHOOL)).thenReturn(List.of(quizResponse));

        mockMvc.perform(get("/api/quizs/mes-publications-web").with(principal(studentUser)))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.length()").value(1));

        verify(quizService).getMesQuizsPublicationWeb(EMAIL_STUDENT_SCHOOL);
    }

    @Test
    void publierQuizReturns200AndBody() throws Exception {
        doNothing().when(quizService).publierQuiz(7L);

        mockMvc.perform(patch("/api/quizs/7/publier"))
                .andExpect(status().isOk())
                .andExpect(content().string("Quiz publié"));

        verify(quizService).publierQuiz(7L);
    }

    @Test
    void publierSurLeWebCallsServiceWithEmailsAndOptionalQuestions() throws Exception {
        PublicationWebRequest body = new PublicationWebRequest();
        body.setEmails(List.of("a@student.com"));
        body.setQuestions(null);

        doNothing().when(quizService).publierSurLeWeb(eq(3L), eq(EMAIL_PROF_SCHOOL), eq(body.getEmails()));

        mockMvc.perform(post("/api/quizs/3/publication-web")
                        .with(principal(profUser))
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(body)))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.message").value("Quiz publié sur le web"))
                .andExpect(jsonPath("$.success").value(true));

        verify(quizService).publierSurLeWeb(3L, EMAIL_PROF_SCHOOL, body.getEmails());
    }

    @Test
    void soumettreQuizReturns200AndScore() throws Exception {
        SoumissionQuizRequest req = new SoumissionQuizRequest();
        req.setEtudiantId(88L);
        req.setReponses(List.of());

        ResultatQuizResponse out = new ResultatQuizResponse();
        out.setScore(75.0);
        out.setBonnesReponses(3);
        out.setTotalQuestions(4);
        out.setEstPremiereTentative(true);

        when(quizService.soumettreQuiz(eq(50L), any(SoumissionQuizRequest.class))).thenReturn(out);

        mockMvc.perform(post("/api/quizs/50/soumettre")
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(req)))
                .andExpect(status().isOk())
                .andExpect(jsonPath(JSON_PATH_SCORE).value(75.0))
                .andExpect(jsonPath("$.bonnesReponses").value(3))
                .andExpect(jsonPath("$.totalQuestions").value(4));

        verify(quizService).soumettreQuiz(eq(50L), any(SoumissionQuizRequest.class));
    }

    @Test
    void getQuizPourPassageWebReturnsBody() throws Exception {
        QuizPassageWebResponse passage = new QuizPassageWebResponse();
        passage.setId(40L);
        passage.setNombreQuestions(1);
        passage.setTitre("Web");

        when(quizService.getQuizPourPassageWeb(40L, EMAIL_STUDENT_SCHOOL)).thenReturn(passage);

        mockMvc.perform(get("/api/quizs/40/passage-web").with(principal(studentUser)))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.titre").value("Web"));

        verify(quizService).getQuizPourPassageWeb(40L, EMAIL_STUDENT_SCHOOL);
    }

    @Test
    void verifierQuestionWebForwardsHeaders() throws Exception {
        VerificationQuestionWebRequest req = new VerificationQuestionWebRequest();
        req.setQuestionId(1L);
        req.setReponseId(2L);

        VerificationQuestionWebResponse resp = VerificationQuestionWebResponse.builder()
                .correcte(true)
                .build();
        when(quizService.verifierQuestionPassageWeb(eq(40L), eq(EMAIL_STUDENT_SCHOOL), any()))
                .thenReturn(resp);

        mockMvc.perform(post("/api/quizs/40/verifier-question-web")
                        .with(principal(studentUser))
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(req)))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.correcte").value(true));

        verify(quizService).verifierQuestionPassageWeb(eq(40L), eq(EMAIL_STUDENT_SCHOOL), any());
    }

    @Test
    void soumettreQuizWebForwardsAccessMode() throws Exception {
        SoumissionQuizWebRequest req = new SoumissionQuizWebRequest();
        req.setReponses(List.of());

        ResultatQuizWebResponse out = new ResultatQuizWebResponse();
        out.setScore(100.0);
        when(quizService.soumettreQuizWeb(eq(12L), eq(EMAIL_STUDENT_SCHOOL), any()))
                .thenReturn(out);

        mockMvc.perform(post("/api/quizs/12/soumettre-web")
                        .with(principal(studentUser))
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(req)))
                .andExpect(status().isOk())
                .andExpect(jsonPath(JSON_PATH_SCORE).value(100.0));

        verify(quizService).soumettreQuizWeb(eq(12L), eq(EMAIL_STUDENT_SCHOOL), any(SoumissionQuizWebRequest.class));
    }

    @Test
    void isPremiereTentativeReturnsBoolean() throws Exception {
        when(quizService.isPremiereTentative(30L, 7L)).thenReturn(true);

        mockMvc.perform(get("/api/quizs/30/premiere-tentative/7"))
                .andExpect(status().isOk())
                .andExpect(content().string("true"));
    }

    @Test
    void syncQuestionsProfCallsService() throws Exception {
        doNothing().when(quizService).synchroniserQuestionsPublicationWeb(eq(7L), eq(EMAIL_PROF_SCHOOL), any());

        var body = Map.of("questions", List.of());
        mockMvc.perform(post("/api/quizs/7/sync-questions-prof")
                        .with(principal(profUser))
                        .contentType(MediaType.APPLICATION_JSON)
                        .content(objectMapper.writeValueAsString(body)))
                .andExpect(status().isOk());

        verify(quizService).synchroniserQuestionsPublicationWeb(eq(7L), eq(EMAIL_PROF_SCHOOL), any());
    }
}
